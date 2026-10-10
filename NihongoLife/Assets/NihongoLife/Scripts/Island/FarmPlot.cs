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
            if (Record.tilled) return "Đất đã được xới rồi.";
            if (IslandEconomy.Owned("tool_hoe") == 0) return "Cần cái cuốc (くわ) — mua ở cửa hàng Midori.";
            var r = Record; r.tilled = true; r.cropId = null; r.stage = 0; r.watered = false;
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Plant(string cropId)
        {
            var crop = IslandCatalog.Load().Crop(cropId);
            if (crop == null) return "Không có loại hạt này.";
            if (!Record.tilled) return "Hãy xới đất bằng cuốc trước.";
            if (!string.IsNullOrEmpty(Record.cropId)) return "Ô này đã có cây.";
            if (!PlayerInventory.Instance.RemoveItem(crop.SeedItemId)) return $"Hết hạt {crop.word.vi} — mua thêm ở cửa hàng.";
            var r = Record; r.cropId = cropId; r.stage = 0; r.watered = false;
            IslandState.Discover("crop:" + cropId);
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Water()
        {
            var phase = CurrentPhase;
            if (phase == Phase.Untilled || phase == Phase.Tilled) return "Chưa có gì để tưới — hãy gieo hạt trước.";
            if (phase == Phase.Ready) return "Cây đã chín, thu hoạch thôi!";
            if (phase == Phase.Growing) return "Cây vừa được tưới, đợi cây lớn đã.";
            if (IslandEconomy.Owned("tool_watering_can") == 0) return "Cần bình tưới (じょうろ).";
            var r = Record; r.watered = true; r.stageStartTicks = IslandState.UtcNow.Ticks;
            IslandState.Save(); Refresh(true);
            return null;
        }

        public string Harvest(out int amount)
        {
            amount = 0;
            if (CurrentPhase != Phase.Ready) return "Cây chưa chín.";
            var crop = Crop;
            if (!IslandEconomy.CanStore(crop.ProduceItemId)) return "Balo đầy — bán bớt nông sản trước.";
            if (!IslandEconomy.Give(crop.ProduceItemId, crop.yield)) return "Balo đầy — bán bớt nông sản trước.";
            amount = crop.yield;
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = false;
            var island = IslandState.Record; island.harvested++;
            IslandState.Discover("crop:" + crop.id);
            IslandState.Save(); Refresh(true);
            IslandAchievements.Check();
            return null;
        }

        public string Clear()
        {
            if (IslandEconomy.Owned("tool_shovel") == 0) return "Cần cái xẻng (シャベル).";
            if (string.IsNullOrEmpty(Record.cropId)) return "Ô đất đang trống.";
            var r = Record; r.cropId = null; r.stage = 0; r.watered = false; r.tilled = true;
            IslandState.Save(); Refresh(true);
            return null;
        }

        // ── Interaction ──

        public string GetPromptJa() => IslandLanguage.Target == TargetLanguage.English ? $"Field {number}" : $"はたけ {number}";
        public string GetpromptEn() => $"Ô ruộng {number} — {PhaseLabel(CurrentPhase)}";
        public Transform GetTransform() => transform;
        public float InteractionPriority => 0.4f;
        public void Interact(GameObject player) => IslandUI.OpenFarm(this);

        public static string PhaseLabel(Phase phase) => phase switch
        {
            Phase.Untilled => "cần xới đất",
            Phase.Tilled => "sẵn sàng gieo hạt",
            Phase.NeedsWater => "cần tưới nước",
            Phase.Growing => "đang lớn",
            Phase.Ready => "đã chín, thu hoạch được",
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
