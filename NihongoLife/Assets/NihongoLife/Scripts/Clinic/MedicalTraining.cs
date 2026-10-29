using System;
using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Clinic
{
    [Serializable]
    public sealed class TrainingChoice
    {
        public string ja, vi, en, whyVi, whyEn;
        public bool correct;
        public string Label => Loc.Pick(vi, en, ja);
        public string Why => Loc.Pick(whyVi, whyEn, whyEn);
    }

    /// <summary>One clinical case of the training room (Resources/Clinic/medical_training.json).</summary>
    [Serializable]
    public sealed class TrainingCase
    {
        public string id, illness, glyph, titleVi, titleEn, titleJa, specialtyVi, specialtyEn, specialtyJa, patientJa, findingsVi, findingsEn, redFlagsVi, redFlagsEn, sourceVi;
        public TrainingChoice[] diagnosis = Array.Empty<TrainingChoice>();
        public TrainingChoice[] treatment = Array.Empty<TrainingChoice>();
        public string Title => Loc.Pick(titleVi, titleEn, titleJa);
        /// <summary>The medical specialty (診療科) the case belongs to.</summary>
        public string Specialty => Loc.Pick(specialtyVi, specialtyEn, specialtyJa);
        public string Findings => Loc.Pick(findingsVi, findingsEn, findingsEn);
        public string RedFlags => Loc.Pick(redFlagsVi, redFlagsEn, redFlagsEn);
        public TrainingChoice RightDiagnosis => diagnosis.FirstOrDefault(c => c.correct);
        public TrainingChoice RightTreatment => treatment.FirstOrDefault(c => c.correct);
    }

    [Serializable]
    public sealed class TrainingCatalog
    {
        public string schema, noteVi;
        public int knowledgePerCase = 15;
        public TrainingCase[] cases = Array.Empty<TrainingCase>();

        private static TrainingCatalog _cached;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _cached = null;

        public static TrainingCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>("Clinic/medical_training");
            _cached = text != null ? JsonUtility.FromJson<TrainingCatalog>(text.text) : new TrainingCatalog();
            _cached.cases ??= Array.Empty<TrainingCase>();
            return _cached;
        }

        public TrainingCase Case(string id) => cases.FirstOrDefault(c => c.id == id);

        /// <summary>Problems in the file (empty when valid): one right diagnosis and one right treatment per case, every
        /// wrong answer explained, red flags and a source given, the illness known to illnesses.json.</summary>
        public List<string> Validate(Func<string, bool> illnessExists)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (var c in cases)
            {
                string at = "case " + c.id + ": ";
                if (string.IsNullOrEmpty(c.id) || !ids.Add(c.id)) errors.Add("case id missing or duplicate: " + c.id);
                if (illnessExists != null && !illnessExists(c.illness)) errors.Add(at + "unknown illness " + c.illness);
                foreach (var (name, set) in new[] { ("diagnosis", c.diagnosis), ("treatment", c.treatment) })
                {
                    if (set.Count(x => x.correct) != 1) errors.Add(at + name + " needs exactly one right answer");
                    if (set.Length < 2) errors.Add(at + name + " needs at least two choices");
                    foreach (var x in set.Where(x => !x.correct)) if (string.IsNullOrWhiteSpace(x.whyVi) || string.IsNullOrWhiteSpace(x.whyEn)) errors.Add(at + name + " wrong answer '" + x.vi + "' is not explained");
                }
                if (string.IsNullOrWhiteSpace(c.redFlagsVi) || string.IsNullOrWhiteSpace(c.sourceVi)) errors.Add(at + "needs red flags and a source");
                if (string.IsNullOrWhiteSpace(c.patientJa) || string.IsNullOrWhiteSpace(c.findingsVi)) errors.Add(at + "needs the patient's words and the findings");
                if (string.IsNullOrWhiteSpace(c.specialtyVi) || string.IsNullOrWhiteSpace(c.specialtyEn) || string.IsNullOrWhiteSpace(c.specialtyJa)) errors.Add(at + "needs a specialty (vi, en, ja)");
            }
            return errors;
        }
    }

    /// <summary>
    /// The training room (研修室) of Hibari Clinic, for the medical career: a case is a patient's words and the findings,
    /// then two questions — the diagnosis, then the treatment as Japanese practice guidelines give it — each wrong
    /// answer explained. A solved case gives knowledge (the medical ladder's promotions need it) and is written into
    /// the doctor's notebook (HandbookUI's 医師の手帳 chapter). Not a medical career: the room explains how to join.
    /// </summary>
    public static class MedicalTraining
    {
        public static TrainingCatalog Catalog => TrainingCatalog.Load();
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        public static bool IsMedical => CareerService.State is { HasCareer: true } s && s.careerId == "medical";

        public static IReadOnlyList<string> Done => (IReadOnlyList<string>)CareerService.State?.trainedCases ?? Array.Empty<string>();
        public static bool IsDone(string caseId) => Done.Contains(caseId);

        /// <summary>The next case to study (the first not yet solved, then round again).</summary>
        public static TrainingCase Next()
        {
            var all = Catalog.cases;
            if (all.Length == 0) return null;
            return all.FirstOrDefault(c => !IsDone(c.id)) ?? all[UnityEngine.Random.Range(0, all.Length)];
        }

        public static void Open()
        {
            if (!IsMedical)
            {
                HudFeed.Post(Loc.F("Phòng đào tạo y khoa (研修室) dành cho nghề Y tế — chọn nghề này từ cấp {0} (sổ nghề nghiệp).", "The medical training room (研修室) is for the medical career — choose it from level {0}.", "研修室は医療キャリア用です（レベル{0}から）。", CareerService.Catalog.unlockLevel), HudFeed.Kind.Info, 7f, "training");
                return;
            }
            Study(Next());
        }

        /// <summary>Step 1: the diagnosis.</summary>
        public static void Study(TrainingCase c)
        {
            if (c == null) return;
            JobQuizCard.Show(Loc.T("研修 · ĐÀO TẠO Y KHOA", "研修 · MEDICAL TRAINING", "研修") + " · " + c.Title + " · " + c.Specialty, Loc.T("Bệnh nhân", "Patient", "かんじゃさん"),
                c.patientJa, null, c.Findings, Loc.T("Bước 1 — chẩn đoán là gì?", "Step 1 — what is the diagnosis?", "ステップ1 — しんだんは？"),
                Choices(c.diagnosis), () => Treat(c), "training");
        }

        /// <summary>Step 2: the treatment; then the case is solved.</summary>
        private static void Treat(TrainingCase c)
        {
            JobQuizCard.Show(Loc.T("研修 · ĐIỀU TRỊ", "研修 · TREATMENT", "研修 · ちりょう"), Loc.T("Bác sĩ Mori", "Dr Mori", "もり せんせい"),
                "では、どう しますか。", null, Loc.F("Chẩn đoán: {0}. Xử trí thế nào?", "Diagnosis: {0}. What do you do?", "診断：{0}", c.RightDiagnosis?.Label),
                Loc.T("Bước 2 — xử trí đúng hướng dẫn:", "Step 2 — treat it by the guidelines:", "ステップ2 — ちりょうは？"), Choices(c.treatment), () => Solve(c), "training");
        }

        /// <summary>Records a solved case: knowledge, the notebook, the red flags shown.</summary>
        public static void Solve(TrainingCase c)
        {
            var state = CareerService.State;
            bool first = state != null && !state.trainedCases.Contains(c.id);
            if (first) state.trainedCases.Add(c.id);
            if (first) PlayerStatus.Instance?.AddKnowledge(Catalog.knowledgePerCase);
            if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository)) repository.SaveProgress(repository.GetProgress());
            HudFeed.Post(Loc.F("Ca \"{0}\" hoàn thành{1}. Dấu hiệu nguy hiểm: {2}", "Case \"{0}\" solved{1}. Red flags: {2}", "症例「{0}」完了{1}。注意：{2}",
                c.Title, first ? Loc.F(" (+{0} trí tuệ, ghi vào sổ tay bác sĩ)", " (+{0} knowledge, in the doctor's notebook)", "（知識+{0}）", Catalog.knowledgePerCase) : "", c.RedFlags), HudFeed.Kind.Reward, 10f, "training");
            Changed?.Invoke();
        }

        private static IList<JobQuizCard.Choice> Choices(TrainingChoice[] set)
        {
            var list = set.Select(x => new JobQuizCard.Choice { Label = x.Label, Detail = Loc.IsJapanese ? null : x.ja, Correct = x.correct, WrongHint = x.Why }).ToList();
            for (int i = list.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
            return list;
        }
    }
}
