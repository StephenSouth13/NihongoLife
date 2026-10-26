using System.Collections.Generic;
using System.Linq;
using NihongoLife.Clinic;
using NihongoLife.Core;
using NihongoLife.UI;

namespace NihongoLife.Progression
{
    /// <summary>
    /// The clinic shift (job_clinic_shift, also the Medical career's work): check patients in at reception (受付),
    /// ask the waiting patients what is wrong (問診) and write it down correctly, and explain at the medicine window
    /// how to take the prescription (お薬の説明), in Japanese. Content: Resources/Clinic/clinic.json.
    /// </summary>
    public sealed partial class JobStation
    {
        private static string ClinicKicker(string place) => Loc.T("LÀM Ở PHÒNG KHÁM", "CLINIC SHIFT", "クリニック勤務") + " · " + place;

        private int ShiftSeed => System.Math.Abs((stationId + QuestService.State(questId).acceptedTicks).GetHashCode());

        private void ClinicCheckIn()
        {
            var choices = new List<JobQuizCard.Choice>
            {
                new JobQuizCard.Choice { Label = "ほけんしょうを おねがいします。", Detail = Loc.T("Cho tôi xem thẻ bảo hiểm.", "Your insurance card, please.", null), Correct = true },
                new JobQuizCard.Choice { Label = "おかいけいは ろっぴゃくえんです。", Detail = Loc.T("Viện phí 600 yên.", "That's ¥600.", null), WrongHint = Loc.T("Bệnh nhân vừa mới đến — chưa tính tiền.", "The patient has only just arrived — no bill yet.", "まだ来たばかりです。") },
                new JobQuizCard.Choice { Label = "おだいじに。", Detail = Loc.T("Chúc mau khoẻ.", "Get well soon.", null), WrongHint = Loc.T("Câu này nói khi bệnh nhân ra về.", "That's for when the patient leaves.", "帰るときの言葉です。") },
            };
            JobQuizCard.Show(ClinicKicker("受付"), Loc.T("Bệnh nhân", "Patient", "かんじゃさん"), "すみません、はじめて なんですが……", "sumimasen, hajimete nan desu ga",
                Loc.T("Xin lỗi, tôi đến lần đầu…", "Excuse me, it's my first visit…", ""), Loc.T("Bạn (lễ tân) nói gì?", "What do you (reception) say?", "受付として なんと いいますか。"),
                choices.OrderBy(c => (c.Label.GetHashCode() & 0x7fffffff) % 7).ToList(), () =>
                {
                    JobShift.MarkDone(stationId);
                    QuestService.Raise("checkin", "clinic_patient");
                    HudFeed.Post("「はい、どうぞ。」" + Loc.T(" — Bệnh nhân đưa thẻ bảo hiểm và ngồi chờ.", " — The patient hands over the card and sits down.", ""), HudFeed.Kind.Info, 3f);
                }, "clinic_card");
        }

        private void ClinicTriage()
        {
            var symptoms = ClinicCatalog.Load().symptoms;
            if (symptoms.Length < 4) return;
            var symptom = symptoms[ShiftSeed % symptoms.Length];
            var choices = symptoms.Where(s => s != symptom).OrderBy(s => (s.id.GetHashCode() & 0x7fffffff) % 11).Take(3).Append(symptom).Select(s => new JobQuizCard.Choice
            {
                Label = s.Meaning, Correct = s == symptom,
                WrongHint = Loc.F("Bệnh nhân nói 「{0}」 ({1}).", "The patient said 「{0}」 ({1}).", "「{0}」", symptom.sayJa, symptom.reading),
            }).OrderBy(c => (c.Label.GetHashCode() & 0x7fffffff) % 13).ToList();
            JobQuizCard.Show(ClinicKicker("問診"), Loc.T("Bệnh nhân", "Patient", "かんじゃさん"), "きのうから " + symptom.sayJa, symptom.reading,
                null, Loc.T("Ghi đúng triệu chứng vào phiếu hỏi bệnh:", "Write the right symptom on the questionnaire:", "問診票に正しく書きましょう："), choices, () =>
                {
                    JobShift.MarkDone(stationId);
                    QuestService.Raise("triage", "clinic_patient");
                    HudFeed.Post(Loc.F("Phiếu hỏi bệnh: {0} → bác sĩ Mori đã nhận.", "Questionnaire: {0} → passed to Dr Mori.", "問診票：{0} → もり先生へ", symptom.Meaning), HudFeed.Kind.Info, 3f);
                }, "clinic_form");
        }

        private void ClinicMedicine()
        {
            var catalog = ClinicCatalog.Load();
            var withDosage = catalog.symptoms.Where(s => catalog.Dosage(s.dosage) != null).ToArray();
            if (withDosage.Length == 0 || catalog.dosages.Length < 4) return;
            var symptom = withDosage[ShiftSeed % withDosage.Length];
            var dosage = catalog.Dosage(symptom.dosage);
            var choices = catalog.dosages.Where(d => d != dosage).Take(3).Append(dosage).Select(d => new JobQuizCard.Choice
            {
                Label = d.ja, Detail = Loc.IsJapanese ? null : d.Meaning, Correct = d == dosage,
                WrongHint = Loc.F("Đơn thuốc ghi: {0}.", "The prescription says: {0}.", "処方せん：{0}", dosage.Meaning),
            }).OrderBy(c => (c.Label.GetHashCode() & 0x7fffffff) % 13).ToList();
            JobQuizCard.Show(ClinicKicker("お薬"), Loc.T("Bệnh nhân", "Patient", "かんじゃさん"), "この くすりは どう のみますか。", "kono kusuri wa dō nomimasu ka",
                Loc.T("Thuốc này uống thế nào ạ?", "How do I take this medicine?", ""),
                Loc.F("Đơn thuốc: {0} — {1}. Giải thích bằng tiếng Nhật:", "Prescription: {0} — {1}. Explain in Japanese:", "処方せん：{0}。説明しましょう：", symptom.Medicine, dosage.Meaning), choices, () =>
                {
                    QuestService.Raise("explain", "clinic_pharmacy");
                    HudFeed.Post("「わかりました。ありがとうございます。」" + Loc.T(" — Bệnh nhân đã hiểu cách dùng thuốc.", " — The patient understands the medicine.", ""), HudFeed.Kind.Info, 3f);
                }, "clinic_medicine");
        }
    }
}
