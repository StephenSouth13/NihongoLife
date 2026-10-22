using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.Island
{
    /// <summary>Partial of IslandUI: barn / animal card. The MonoBehaviour entry point stays IslandUI.cs.</summary>
    public sealed partial class IslandUI : MonoBehaviour
    {
        // ─────────── Animal card ───────────

        public static bool AnimalOpen => _instance != null && _instance._animalCard != null;
        private IslandAnimal _animal;

        public static void OpenAnimal(IslandAnimal animal)
        {
            var ui = Instance;
            bool wasOpen = ui._animalCard != null;
            ui._animal = animal;
            ui.BuildAnimal(null, false);
            if (!wasOpen)
            {
                ui.LockPlayer(true);
                UiModalStack.Register(ui, () => ui._animalCard != null, () => ui.Close(ref ui._animalCard), "Animal");
            }
        }

        public static void CloseAnimal() { if (_instance != null) _instance.Close(ref _instance._animalCard); }

        private void BuildAnimal(string feedback, bool error)
        {
            Discard(_animalCard);
            var def = _animal.Def;
            var verbs = IslandCatalog.Load().verbs;
            _animalCard = Card("AnimalCard", new Vector2(1f, 0.5f), new Vector2(-28f, 20f), 460f);
            Header(_animalCard, "pets", IslandLanguage.Title(def.word), Loc.T("CHUỒNG TRẠI", "BARN", "なや"), CloseAnimal);
            var body = Body(_animalCard);
            WordChip(body, def.word, 20f);
            Text(body, $"「{IslandLanguage.Sound(def.sound)}」", 22f, Green, FontStyles.Italic);
            var actions = NLUi.Group(body, "Actions", true, 8f);
            if (def.foods != null && def.foods.Length > 0)
            {
                PhraseCard(body, verbs.feed);
                string foods = string.Join(" / ", def.foods.Select(f => IslandLanguage.Name(IslandCatalog.Load().Crop(f)?.word)));
                var feed = IconBtn(actions, "Feed", "nutrition", $"{Loc.T("Cho ăn", "Feed", "えさをあげる")}  ·  {foods}", () => Work(Loc.T("Đang cho ăn…", "Feeding…", "えさやり中…"), Loc.T("tay", "hands", "手"), FarmActionTimes.Feed, pose: "Work_Feed", done: () =>
                {
                    if (_animalCard == null) return;
                    string err = _animal.Feed(out string food);
                    if (err != null) { BuildAnimal(err, true); return; }
                    IslandState.Discover("verb:" + verbs.feed.en);
                    BuildAnimal(Loc.F("{0} ăn ngon lành!", "The {0} enjoys it!", "{0}がおいしそうに食べました！", IslandLanguage.Name(def.word)), false);
                }), Green, Color.white, 50f, 16.5f);
            }
            var pet = IconBtn(actions, "Pet", "favorite", Loc.T("Vuốt ve", "Pet", "なでる"), () =>
            {
                _animal.Pet();
                IslandState.Discover("verb:" + verbs.pet.en);
                BuildAnimal(Loc.F("{0} rất vui.", "The {0} is happy.", "{0}はうれしそうです。", IslandLanguage.Name(def.word)), false);
            }, Cream2, Green, 46f, 16f);
            Text(body, feedback ?? "", 15f, error ? Error : Green, FontStyles.Bold);
        }
    }
}
