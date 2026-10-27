using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.PhuQuoc
{
    public enum GanhNgonStation { Table, Reception, Kitchen, Bar, Manager }

    /// <summary>
    /// A shift at Gánh Ngon, the same way the clinic shift works (ClinicVisit + JobQuizCard): every station
    /// plays a pre-written dialogue from ganh_ngon.json one step at a time; a right answer raises the step's
    /// quest event (order / serve) on the dialogue's target, so job_ganhngon_shift (progression.json) and the
    /// "hospitality" career (careers.json) advance by themselves. English-speaking guests are answered in English.
    /// </summary>
    public static class GanhNgonShift
    {
        public const string ShiftQuest = "job_ganhngon_shift";
        public const string ManagerId = "npc_ganhngon_manager";

        private static readonly Dictionary<string, int> Progress = new Dictionary<string, int>();
        private static int _rotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Progress.Clear(); _rotation = 0; }

        public static string JobOf(GanhNgonStation s) => s switch
        {
            GanhNgonStation.Reception => "receptionist",
            GanhNgonStation.Kitchen => "chef",
            GanhNgonStation.Bar => "bartender",
            _ => "waiter",
        };

        /// <summary>The dialogue a station plays next (rotates through the job's dialogues).</summary>
        public static GanhNgonDialogue NextDialogue(GanhNgonCatalog catalog, GanhNgonStation station, int rotation)
        {
            var list = catalog.DialoguesFor(JobOf(station)).ToList();
            return list.Count == 0 ? null : list[Mathf.Abs(rotation) % list.Count];
        }

        /// <summary>Answers for a step in the guest's language (English guests: English labels).</summary>
        public static List<JobQuizCard.Choice> Choices(GanhNgonDialogue d, GanhNgonStep s)
        {
            bool en = d.guestLanguage == "en";
            return s.choices.Select(c => new JobQuizCard.Choice
            {
                Label = en ? c.en : c.ja,
                Detail = en ? Loc.T(c.vi, null, c.ja) : Loc.T(c.vi, c.en, null),
                Correct = c.correct,
                WrongHint = c.correct ? null : c.Hint,
            }).ToList();
        }

        public static void Use(GanhNgonStation station)
        {
            var catalog = GanhNgonCatalog.Load();
            if (station == GanhNgonStation.Manager)
            {
                if (QuestService.ReadyToReport(ShiftQuest)) QuestService.Raise("talk", ManagerId);
                HudFeed.Post(Loc.T("Chị Mai: 「おつかれさまでした！」 Ca làm hôm nay thế nào?", "Mai: “Otsukaresama deshita!” How was the shift?", "マイさん：「おつかれさまでした！」"), HudFeed.Kind.Info, 6f);
                return;
            }
            var dialogue = NextDialogue(catalog, station, _rotation);
            if (dialogue == null) return;
            Progress.TryGetValue(dialogue.id, out int index);
            if (index >= dialogue.steps.Length) index = 0;
            var step = dialogue.steps[index];
            bool en = dialogue.guestLanguage == "en";
            string line = en ? step.lineEn : step.lineJa;
            string reading = en || Loc.IsJapanese ? null : step.readingJa;
            string meaning = en ? Loc.T(step.lineVi, null, step.lineJa) : Loc.T(step.lineVi, step.lineEn, "");
            string kicker = Loc.T("GÁNH NGON", "GÁNH NGON", "ガインゴン") + " · " + dialogue.Guest;
            JobQuizCard.Show(kicker, dialogue.Guest, line, reading, meaning, Loc.Pick(step.questVi, step.questEn, step.questJa),
                Choices(dialogue, step).OrderBy(_ => Random.value).ToList(), () =>
                {
                    if (!string.IsNullOrEmpty(step.questEvent)) QuestService.Raise(step.questEvent, dialogue.target);
                    Progress[dialogue.id] = index + 1;
                    if (index + 1 >= dialogue.steps.Length) { Progress[dialogue.id] = 0; _rotation++; }
                });
        }
    }

    /// <summary>A service station in Gánh Ngon (tables, reception, kitchen pass, pub bar, the shift manager).</summary>
    public sealed class GanhNgonSpot : MonoBehaviour, IInteractable
    {
        public GanhNgonStation station;
        public string GetPromptJa() => station switch
        {
            GanhNgonStation.Reception => "うけつけ",
            GanhNgonStation.Kitchen => "ちゅうぼう",
            GanhNgonStation.Bar => "バー",
            GanhNgonStation.Manager => "マイさん",
            _ => "テーブル",
        };
        public string GetpromptEn() => station switch
        {
            GanhNgonStation.Reception => Loc.T("Lễ tân", "Reception", "受付"),
            GanhNgonStation.Kitchen => Loc.T("Bếp", "Kitchen pass", "厨房"),
            GanhNgonStation.Bar => Loc.T("Quầy pub", "Pub bar", "バー"),
            GanhNgonStation.Manager => Loc.T("Chị Mai (quản lý ca)", "Mai (shift manager)", "マイさん"),
            _ => Loc.T("Bàn khách", "Guest table", "テーブル"),
        };
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => GanhNgonShift.Use(station);
    }
}
