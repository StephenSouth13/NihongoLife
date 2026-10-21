using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using TMPro;
using UnityEngine;

namespace NihongoLife.Island
{
    /// <summary>
    /// One plot of the field. States (saved in FarmPlotRecord, so they survive leaving the island):
    /// untilled → tilled (hoe) → seed planted → watered → grows for the crop's secondsPerStage → needs water again →
    /// … → ready (stage 3) → harvested (soil must be tilled again). The shovel clears a plot.
    /// Growth is computed from saved UTC timestamps, so crops keep growing while the player is away.
    /// </summary>
    public sealed class FarmPlot : MonoBehaviour, IInteractable, IInteractionPriority
    {
        public enum Phase { Untilled, Tilled, NeedsWater, Growing, Ready }

        [SerializeField] private string plotId = "plot_1";
        [SerializeField] private int number = 1;
        [SerializeField] private Renderer soil;
        [SerializeField] private Transform cropAnchor;
        [SerializeField] private TextMeshPro marker;

        private GameObject _cropModel;
        private string _shownModel;
        private float _nextTick;
        private static readonly Color GrassSoil = new Color(0.45f, 0.52f, 0.28f);
        private static readonly Color DrySoil = new Color(0.55f, 0.38f, 0.24f);
        private static readonly Color WetSoil = new Color(0.33f, 0.22f, 0.15f);
        private MaterialPropertyBlock _block;

        public string PlotId => plotId;
        public int Number => number;
        public FarmPlotRecord Record => IslandState.Plot(plotId);
        public IslandCrop Crop => string.IsNullOrEmpty(Record.cropId) ? null : IslandCatalog.Load().Crop(Record.cropId);

        public void Configure(string id, int plotNumber, Renderer soilRenderer, Transform anchor, TextMeshPro statusMarker)
        {
            plotId = id; number = plotNumber; soil = soilRenderer; cropAnchor = anchor; marker = statusMarker;
        }

        public Phase CurrentPhase
        {
            get
            {
                Advance();
                var r = Record;
                if (!r.tilled) return Phase.Untilled;
                if (string.IsNullOrEmpty(r.cropId)) return Phase.Tilled;
                if (r.stage >= 3) return Phase.Ready;
                return r.watered ? Phase.Growing : Phase.NeedsWater;
            }
        }

        /// <summary>0..1 progress of the current growing stage.</summary>
        public float StageProgress
        {
            get
            {
                var r = Record; var crop = Crop;
                if (crop == null || !r.watered || r.stage >= 3) return r.stage >= 3 ? 1f : 0f;
                double seconds = (IslandState.UtcNow - new DateTime(r.stageStartTicks, DateTimeKind.Utc)).TotalSeconds;
                return Mathf.Clamp01((float)(seconds / Mathf.Max(1f, crop.secondsPerStage)));
            }
        }

        public float SecondsLeft
        {
            get { var crop = Crop; return crop == null ? 0f : (1f - StageProgress) * crop.secondsPerStage; }
        }

        /// <summary>Applies growth that happened since the last check (also while the player was away).</summary>
        public void Advance()
        {
            var r = Record; var crop = Crop;
            if (crop == null || !r.watered || r.stage >= 3) return;
            var start = new DateTime(r.stageStartTicks, DateTimeKind.Utc);
            if ((IslandState.UtcNow - start).TotalSeconds < crop.secondsPerStage) return;
            r.stage++;
            r.watered = false;     // each stage needs fresh water (the ready stage does not)
            IslandState.Save();
        }

        // ── Actions (return a short Vietnamese error, or null on success) ──

        public string Till()
        {
            if (Record.tilled) return Loc.T("Đất đã được xới rồi.", "The soil is already tilled.", "もう耕してあります。");
            string tool = FarmActionTimes.Till().toolId;
            if (tool == null) return Loc.T("Cần cuốc hoặc xẻng để xới — mua ở cửa hàng Midori.", "You need a hoe or a shovel to till — buy one at the Midori Store.", "耕すにはくわかシャベルが必要です。みどりしょうてんで買えます。");
            var r = Record; r.tilled = true; r.cropId = null; r.stage = 0; r.watered = false;
            IslandState.Save(); Refresh(true);
            IslandTools.Wear(tool);
            NihongoLife.Progression.QuestService.Raise("till", plotId);
            return null;
        }

        public string Plant(string cropId)
        {
            var crop = IslandCatalog.Load().Crop(cropId);
            if (crop == null) return Loc.T("Không có loại hạt này.", "There is no such seed.", "その種はありません。");
            if (!Record.tilled) return Loc.T("Hãy xới đất bằng cuốc trước.", "Till the soil with a hoe first.", "先にくわで耕しましょう。");
            if (!string.IsNullOrEmpty(Record.cropId)) return Loc.T("Ô này đã có cây.", "Something is already growing here.", "ここにはもう植えてあります。");
            if (!PlayerInventory.Instance.RemoveItem(crop.SeedItemId)) return Loc.F("Hết hạt {0} — mua thêm ở cửa hàng.", "Out of {0} seeds — buy more at the store.", "{0}の種がありません。お店で買いましょう。", IslandLanguage.Name(crop.word));
            var r = Record; r.cropId = cropId; r.stage = 0; r.watered = false;
            IslandState.Discover("crop:" + cropId);
            IslandState.Save(); Refresh(true);
            NihongoLife.Progression.QuestService.Raise("plant", cropId);
            return null;
        }

        public string Water()
        {
            var phase = CurrentPhase;
            if (phase == Phase.Untilled || phase == Phase.Tilled) return Loc.T("Chưa có gì để tưới — hãy gieo hạt trước.", "Nothing to water yet — plant seeds first.", "まだ水をあげるものがありません。先に種をまきましょう。");
            if (phase == Phase.Ready) return Loc.T("Cây đã chín, thu hoạch thôi!", "It is ripe — harvest it!", "実りました。収穫しましょう！");
            if (phase == Phase.Growing) return Loc.T("Cây vừa được tưới, đợi cây lớn đã.", "Just watered — wait for it to grow.", "水やりしたばかりです。育つのを待ちましょう。");
            if (!IslandTools.Usable("tool_watering_can")) return Loc.T("Cần bình tưới — mua ở cửa hàng Midori.", "You need a watering can — buy one at the Midori Store.", "じょうろが必要です。みどりしょうてんで買えます。");
            var r = Record; r.watered = true; r.stageStartTicks = IslandState.UtcNow.Ticks;
            IslandState.Save(); Refresh(true);
            IslandTools.Wear("tool_watering_can");
            NihongoLife.Progression.QuestService.Raise("water", r.cropId);
            return null;
        }

        public string Harvest(out int amount)
        {
            amount = 0;
            if (CurrentPhase != Phase.Ready) return Loc.T("Cây chưa chín.", "Not ripe yet.", "まだ実っていません。");
            var crop = Crop;
            if (!IslandEconomy.CanStore(crop.ProduceItemId)) return Loc.T("Balo đầy — bán bớt nông sản trước.", "Your bag is full — sell some produce first.", "バッグがいっぱいです。先に作物を売りましょう。");
            if (!IslandEconomy.Give(crop.ProduceItemId, crop.yield)) return Loc.T("Balo đầy — bán bớt nông sản trước.", "Your bag is full — sell some produce first.", "バッグがいっぱいです。先に作物を売りましょう。");
            amount = crop.yield;
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = false;
            var island = IslandState.Record; island.harvested++;
            IslandState.Discover("crop:" + crop.id);
            IslandState.Save(); Refresh(true);
            IslandAchievements.Check();
            NihongoLife.Progression.QuestService.Raise("harvest", crop.id);
            return null;
        }

        public string Clear()
        {
            if (string.IsNullOrEmpty(Record.cropId)) return Loc.T("Ô đất đang trống.", "The plot is empty.", "畑は空いています。");
            string tool = FarmActionTimes.Clear().toolId;
            if (tool == null) return Loc.T("Cần xẻng hoặc cuốc để dọn ô.", "You need a shovel or a hoe to clear the plot.", "畑を片づけるにはシャベルかくわが必要です。");
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = true;
            IslandState.Save(); Refresh(true);
            IslandTools.Wear(tool);
            return null;
        }

        // ── Interaction ──

        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? $"Field {number}" : $"はたけ {number}";
        public string GetpromptEn() => Loc.F("Ô ruộng {0} — {1}", "Field {0} — {1}", "はたけ{0} — {1}", number, PhaseLabel(CurrentPhase));
        public Transform GetTransform() => transform;
        public float InteractionPriority => 0.4f;
        public void Interact(GameObject player) { if (!IslandVisit.ReadOnlyNotice()) IslandUI.OpenFarm(this); }

        public static string PhaseLabel(Phase phase) => phase switch
        {
            Phase.Untilled => Loc.T("cần xới đất", "needs tilling", "耕す必要あり"),
            Phase.Tilled => Loc.T("sẵn sàng gieo hạt", "ready for seeds", "種まきできます"),
            Phase.NeedsWater => Loc.T("cần tưới nước", "needs water", "水やりが必要"),
            Phase.Growing => Loc.T("đang lớn", "growing", "成長中"),
            Phase.Ready => Loc.T("đã chín, thu hoạch được", "ripe — ready to harvest", "収穫できます"),
            _ => "",
        };

        // ── Visuals ──

        private void Start() => Refresh(true);

        private void Update()
        {
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 0.5f;
            int stageBefore = Record.stage;
            Advance();
            Refresh(Record.stage != stageBefore);
        }

        public void Refresh(bool force)
        {
            var r = Record;
            var phase = CurrentPhase;
            if (soil != null)
            {
                _block ??= new MaterialPropertyBlock();
                soil.GetPropertyBlock(_block);
                Color c = !r.tilled ? GrassSoil : (phase == Phase.Growing ? WetSoil : DrySoil);
                _block.SetColor("_BaseColor", c);
                soil.SetPropertyBlock(_block);
                soil.transform.localScale = new Vector3(soil.transform.localScale.x, r.tilled ? 0.18f : 0.08f, soil.transform.localScale.z);
            }
            string model = null;
            var crop = Crop;
            if (crop != null) model = $"{crop.model}_{Mathf.Clamp(r.stage + 1, 1, 4)}";
            if (force || model != _shownModel)
            {
                if (_cropModel != null) Destroy(_cropModel);
                _cropModel = null;
                _shownModel = model;
                var prefab = model != null ? IslandModelLibrary.Instance?.Find(model) : null;
                if (prefab != null && cropAnchor != null)
                {
                    _cropModel = Instantiate(prefab, cropAnchor, false);
                    _cropModel.name = model;
                    foreach (var collider in _cropModel.GetComponentsInChildren<Collider>()) Destroy(collider);
                }
            }
            if (marker != null)
            {
                marker.text = phase == Phase.NeedsWater ? "<color=#2F7FC0>みず</color>" : phase == Phase.Ready ? "<color=#E0A020>★</color>" : "";
                marker.gameObject.SetActive(marker.text.Length > 0);
            }
        }
    }
}
