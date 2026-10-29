using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Interaction;
using NihongoLife.Player;
using NihongoLife.Progression;
using NihongoLife.UI;
using UnityEngine;

namespace NihongoLife.Clinic
{
    public enum ClinicSpotKind { Reception, Doctor, Pharmacy, Scale, EyeChart, Training }

    /// <summary>
    /// A visit to Hibari Clinic as a patient, the way a Japanese clinic works: reception (初診, 保険証) and the
    /// questionnaire (問診票) → the doctor (どうしましたか) → paying (会計) → medicine and how to take it (お薬).
    /// The purpose of the visit follows the active clinic task: a health check (健康診断: height, eye test) or a
    /// vaccination (予防接種); otherwise it is a consultation for how the player feels (a cold when soaked, tired
    /// when health is low). Seeing the doctor heals fully and cures the cold. Every step is a Japanese question and
    /// reports to QuestService, so the clinic task line (progression.json, category "clinic") advances by itself.
    /// </summary>
    public static class ClinicVisit
    {
        public enum Stage { None, CheckedIn, Examined, Paid }
        public enum Purpose { Consult, HealthCheck, Vaccine }

        public const string FirstVisitQuest = "clinic_first_visit";
        public const string HealthCheckQuest = "clinic_health_check";
        public const string VaccineQuest = "clinic_vaccine";
        public const string DoctorId = "npc_clinic_doctor";
        public const string NurseId = "npc_clinic_nurse";

        public static Stage Current { get; private set; }
        public static Purpose Reason { get; private set; }
        public static ClinicSymptom Symptom { get; private set; }
        public static bool HealthCheckMeasured { get; private set; }
        public static int EyeRounds { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Current = Stage.None; Reason = Purpose.Consult; Symptom = null; HealthCheckMeasured = false; EyeRounds = 0; Express = null; }

        private static ClinicCatalog Catalog => ClinicCatalog.Load();
        private static bool Active(string questId) => QuestService.State(questId)?.status == "active";
        private static string Kicker(string place) => Loc.T("PHÒNG KHÁM", "CLINIC", "クリニック") + " · " + place;
        private static string Nurse => Loc.T("Điều dưỡng Sato", "Nurse Sato", "かんごしの さとうさん");
        private static string Doctor => Loc.T("Bác sĩ Mori", "Dr Mori", "もり せんせい");

        /// <summary>What the player has today: a cold when soaked, exhaustion when health is low, else a common complaint.</summary>
        public static ClinicSymptom TodaysSymptom()
        {
            var status = PlayerStatus.Instance;
            var all = Catalog.symptoms;
            if (all.Length == 0) return null;
            // A real illness (Health/PlayerIllness) shows as its own symptom.
            var ill = NihongoLife.Health.PlayerIllness.Main;
            if (ill != null)
                foreach (var id in ill.symptoms)
                {
                    var match = Catalog.Symptom(id);
                    if (match != null) return match;
                }
            if (status != null && status.HasCold) return all.FirstOrDefault(s => s.cold) ?? all[0];
            if (status != null && status.CurrentHealth < status.MaxHealth * 0.6f) return Catalog.Symptom("tired") ?? all[0];
            int day = DayNightCycle.Active != null ? DayNightCycle.Active.Day : 0;
            var mild = all.Where(s => !s.cold).ToArray();
            return mild.Length > 0 ? mild[day % mild.Length] : all[0];
        }

        public static string Prompt(ClinicSpotKind kind) => kind switch
        {
            ClinicSpotKind.Reception => Current == Stage.Examined ? Loc.T("Thanh toán (会計)", "Pay the bill (会計)", "お会計") : Loc.T("Quầy tiếp nhận (受付)", "Reception (受付)", "受付"),
            ClinicSpotKind.Doctor => Loc.T("Vào phòng khám bác sĩ", "See the doctor", "診察室に入る"),
            ClinicSpotKind.Pharmacy => Loc.T("Nhận thuốc (お薬)", "Collect medicine (お薬)", "お薬を受け取る"),
            ClinicSpotKind.Scale => Loc.T("Đo chiều cao (身長)", "Height check (身長)", "身長をはかる"),
            ClinicSpotKind.Training => Loc.T("Phòng đào tạo y khoa (研修)", "Medical training (研修)", "研修室で 学ぶ"),
            _ => Loc.T("Đo thị lực (視力)", "Eye test (視力)", "視力検査"),
        };

        public static void Use(ClinicSpotKind kind)
        {
            switch (kind)
            {
                case ClinicSpotKind.Reception: if (Current == Stage.Examined) Pay(); else CheckIn(); break;
                case ClinicSpotKind.Doctor: SeeDoctor(); break;
                case ClinicSpotKind.Pharmacy: CollectMedicine(); break;
                case ClinicSpotKind.Scale: MeasureHeight(); break;
                case ClinicSpotKind.EyeChart: EyeTest(); break;
                case ClinicSpotKind.Training: MedicalTraining.Open(); break;
            }
        }

        // ─────────── Reception (受付) and the questionnaire (問診票) ───────────

        private static void CheckIn()
        {
            if (Current == Stage.CheckedIn) { Hint(Loc.T("Đã đăng ký — vào phòng khám gặp bác sĩ Mori nhé.", "You're checked in — go in and see Dr Mori.", "受付済みです。診察室へどうぞ。")); return; }
            if (Current == Stage.Paid) { Hint(Loc.T("Đã thanh toán — nhận thuốc ở quầy お薬.", "Paid — collect your medicine at the お薬 window.", "お薬の窓口へどうぞ。")); return; }
            Reason = Active(VaccineQuest) ? Purpose.Vaccine : Active(HealthCheckQuest) ? Purpose.HealthCheck : Purpose.Consult;
            if (Reason == Purpose.Consult && !Active(FirstVisitQuest) && !Express.HasValue)
            {
                // A patient who only wants to get better is not made to walk five counters: one card, done.
                JobQuizCard.Show(Kicker("受付"), Nurse, "きょうは どう されますか。", null, Loc.T("Hôm nay anh/chị muốn khám thế nào?", "How would you like to be seen today?", ""),
                    Loc.T("Chọn cách khám:", "Choose:", "えらんでください："), new List<JobQuizCard.Choice>
                    {
                        new JobQuizCard.Choice { Id = "quick", Label = Loc.T("Khám nhanh", "Quick visit", "すぐ みてもらう"), Detail = Loc.T("Một bước: khám, nhận thuốc, thanh toán (hoặc trả sau).", "One step: see the doctor, get the medicine, pay (or later).", null), Correct = true },
                        new JobQuizCard.Choice { Id = "full", Label = Loc.T("Khám từng bước (luyện tiếng Nhật)", "Step by step (practise Japanese)", "ひとつずつ（日本語の れんしゅう）"), Detail = Loc.T("受付 → 問診票 → 診察 → 会計 → お薬", "受付 → 問診票 → 診察 → 会計 → お薬", null), Correct = true },
                    }, (System.Action<JobQuizCard.Choice>)(picked => { Express = picked.Id == "quick"; if (Express == true) QuickVisit(); else CheckIn(); }), "clinic_card");
                return;
            }
            Express = null;
            var choices = new List<JobQuizCard.Choice>
            {
                new JobQuizCard.Choice { Label = "はい。ほけんしょうです。おねがいします。", Detail = Loc.T("Vâng, đây là thẻ bảo hiểm. Nhờ chị ạ.", "Yes, here's my insurance card.", null), Correct = true },
                new JobQuizCard.Choice { Label = "いただきます。", Detail = Loc.T("Mời mọi người ăn.", "Let's eat.", null), WrongHint = Loc.T("Câu này nói trước bữa ăn. Hãy đưa 保険証.", "That's said before a meal. Hand over your 保険証.", "食事の前のあいさつです。") },
                new JobQuizCard.Choice { Label = "おだいじに。", Detail = Loc.T("Chúc mau khoẻ.", "Get well soon.", null), WrongHint = Loc.T("Câu này nhân viên nói với bệnh nhân.", "Staff say that to patients.", "スタッフが患者さんに言う言葉です。") },
            };
            JobQuizCard.Show(Kicker("受付"), Nurse, "こんにちは。ほけんしょうを おねがいします。", "konnichiwa. hokenshō o onegaishimasu",
                Loc.T("Xin chào. Cho tôi xem thẻ bảo hiểm.", "Hello. Your insurance card, please.", ""), Loc.T("Bạn trả lời:", "You answer:", "なんと こたえますか。"), Shuffle(choices), Questionnaire, "clinic_card");
        }

        private static void Questionnaire()
        {
            List<JobQuizCard.Choice> choices;
            string line, meaning, question;
            if (Reason == Purpose.Consult)
            {
                Symptom = TodaysSymptom();
                if (Symptom == null) return;
                choices = Catalog.symptoms.Where(s => s != Symptom).Take(3).Append(Symptom).Select(s => new JobQuizCard.Choice
                {
                    Label = s.sayJa, Detail = Loc.IsJapanese ? null : s.reading, Correct = s == Symptom,
                    WrongHint = Loc.F("「{0}」 nghĩa là: {1}", "「{0}」 means: {1}", "「{0}」", s.sayJa, s.Meaning),
                }).ToList();
                line = "もんしんひょうに きょうの しょうじょうを かいて ください。";
                meaning = Loc.T("Hãy ghi triệu chứng hôm nay vào phiếu hỏi bệnh.", "Write today's symptoms on the questionnaire.", "");
                question = Loc.F("Hôm nay bạn: {0} Đánh dấu dòng đúng:", "Today: {0} Tick the right line:", "今日のようす：{0}", Symptom.Meaning);
            }
            else
            {
                string right = Reason == Purpose.Vaccine ? "よぼうせっしゅの よやくを しました。" : "けんこうしんだんを うけに きました。";
                choices = new List<JobQuizCard.Choice>
                {
                    new JobQuizCard.Choice { Label = "よぼうせっしゅの よやくを しました。", Detail = Loc.T("Tôi đã đặt lịch tiêm phòng.", "I booked a vaccination.", null), Correct = right.StartsWith("よぼう"), WrongHint = Loc.T("Hôm nay bạn đến khám sức khoẻ.", "You're here for a health check.", "") },
                    new JobQuizCard.Choice { Label = "けんこうしんだんを うけに きました。", Detail = Loc.T("Tôi đến khám sức khoẻ.", "I've come for a health check.", null), Correct = right.StartsWith("けんこう"), WrongHint = Loc.T("Hôm nay bạn đến tiêm phòng.", "You're here for a vaccination.", "") },
                    new JobQuizCard.Choice { Label = "かいものに きました。", Detail = Loc.T("Tôi đến mua sắm.", "I came shopping.", null), WrongHint = Loc.T("Đây là phòng khám, không phải siêu thị.", "This is a clinic, not a shop.", "") },
                };
                line = "きょうは どう されましたか。";
                meaning = Loc.T("Hôm nay anh/chị đến vì việc gì?", "What brings you in today?", "");
                question = Loc.T("Bạn đến để làm gì?", "Why are you here?", "なんの ために きましたか。");
            }
            JobQuizCard.Show(Kicker("問診票"), Nurse, line, null, meaning, question, Shuffle(choices), () =>
            {
                Current = Stage.CheckedIn;
                QuestService.Raise("clinic_reception", "clinic_desk");
                Hint(Reason == Purpose.HealthCheck
                    ? Loc.T("Đo chiều cao ở bàn cân và đo thị lực ở bảng chữ C, rồi gặp bác sĩ Mori.", "Measure your height at the scale and do the eye test, then see Dr Mori.", "身長と視力をはかってから、もり先生へ。")
                    : Loc.T("Mời ngồi chờ — bác sĩ Mori sẽ gọi. Vào phòng khám (診察室) khi sẵn sàng.", "Please wait — Dr Mori will call you. Go into the consultation room (診察室).", "おかけになってお待ちください。"), 6f);
            }, "clinic_form");
        }

        /// <summary>The patient's choice at reception this time (null = not asked yet).</summary>
        private static bool? Express;

        /// <summary>
        /// The quick visit: the doctor sees the player at once and treats what they have (the same treatment as the
        /// step-by-step visit), the medicine is handed over, the fee is paid — or billed for later when the wallet is
        /// short. Every clinic task still counts the steps.
        /// </summary>
        public static void QuickVisit()
        {
            Express = null;
            var symptom = TodaysSymptom();
            var illness = NihongoLife.Health.PlayerIllness.Main;
            PlayerStatus.Instance?.TreatAtClinic();
            int fee = Catalog.visitFeeYen;
            var inventory = PlayerInventory.Instance;
            bool paid = inventory != null && inventory.SpendYen(fee, Loc.Triple("Viện phí phòng khám", "Clinic fee", "診察料"));
            if (!paid) NihongoLife.Bank.BankService.AddClinicBill(fee);
            foreach (var (evt, target) in new[] { ("clinic_reception", "clinic_desk"), ("clinic_consult", "clinic_doctor"), ("clinic_pay", "clinic_desk"), ("clinic_medicine", "clinic_pharmacy") })
                QuestService.Raise(evt, target);
            Current = Stage.None;
            Symptom = null;
            string what = symptom != null ? Loc.F("Chẩn đoán: {0}. Thuốc: {1}.", "Diagnosis: {0}. Medicine: {1}.", "診断：{0}。お薬：{1}。", symptom.Diagnosis, symptom.Medicine) : "";
            string how = illness != null ? " " + illness.Clinic : "";
            string bill = paid ? Loc.F(" Viện phí {0}.", " Fee {0}.", " 診察料{0}。", Loc.Yen(fee)) : Loc.F(" Viện phí {0} ghi trả sau (ví ¥ → Ngân hàng).", " The {0} fee is billed for later (Wallet ¥ → Bank).", " {0}は後払い。", Loc.Yen(fee));
            HudFeed.Post("「おだいじに。」 " + what + how + bill, HudFeed.Kind.Reward, 10f, "clinic-quick");
        }

        // ─────────── The doctor (診察室) ───────────

        private static void SeeDoctor()
        {
            // A clinic shift ready to report: the doctor pays (talk objective).
            if (QuestService.ReadyToReport("job_clinic_shift")) { QuestService.Raise("talk", DoctorId); Hint("「おつかれさまでした！」 — " + Loc.T("Bác sĩ Mori cảm ơn bạn.", "Dr Mori thanks you.", "")); return; }
            if (Current != Stage.CheckedIn) { Hint(Loc.T("Hãy đăng ký ở quầy tiếp nhận (受付) trước.", "Check in at reception (受付) first.", "先に受付をしてください。")); return; }
            switch (Reason)
            {
                case Purpose.HealthCheck: HealthCheckResult(); return;
                case Purpose.Vaccine: Vaccinate(); return;
            }
            var symptom = Symptom ?? TodaysSymptom();
            var choices = Catalog.symptoms.Where(s => s != symptom).Take(3).Append(symptom).Select(s => new JobQuizCard.Choice
            {
                Label = s.sayJa, Detail = Loc.IsJapanese ? null : s.reading, Correct = s == symptom,
                WrongHint = Loc.F("Hôm nay bạn: {0}", "Today you: {0}", "今日は：{0}", symptom.Meaning),
            }).ToList();
            JobQuizCard.Show(Kicker("診察室"), Doctor, "こんにちは。きょうは どうしましたか。", "konnichiwa. kyō wa dō shimashita ka",
                Loc.T("Chào bạn. Hôm nay bạn bị sao?", "Hello. What seems to be the problem today?", ""),
                Loc.F("Nói với bác sĩ (bạn: {0}):", "Tell the doctor (you: {0}):", "先生に伝えましょう：", symptom.Meaning), Shuffle(choices), () =>
                {
                    var illness = NihongoLife.Health.PlayerIllness.Main;
                    PlayerStatus.Instance?.TreatAtClinic();
                    Current = Stage.Examined;
                    if (illness != null)
                        HudFeed.Post(Loc.T("Bác sĩ: ", "Doctor: ", "先生：") + illness.Name + " — " + illness.Clinic, HudFeed.Kind.Info, 9f, "clinic-treatment");
                    QuestService.Raise("clinic_consult", "clinic_doctor");
                    HudFeed.Post($"「{symptom.diagnosisJa}ですね。{symptom.medicineJa}を だします。」  " + Loc.F("Chẩn đoán: {0}. Thuốc: {1}. Sức khoẻ đã hồi phục — thanh toán ở quầy.", "Diagnosis: {0}. Medicine: {1}. You feel better — pay at reception.", "診断：{0}。お薬：{1}。受付でお会計を。", symptom.Diagnosis, symptom.Medicine), HudFeed.Kind.Reward, 7f);
                }, "clinic_doctor");
        }

        private static void Vaccinate()
        {
            var choices = new List<JobQuizCard.Choice>
            {
                new JobQuizCard.Choice { Label = "はい、ありません。おねがいします。", Detail = Loc.T("Vâng, không sốt. Nhờ bác sĩ.", "No fever. Please go ahead.", null), Correct = true },
                new JobQuizCard.Choice { Label = "はい、さんじゅうきゅうどです。", Detail = Loc.T("Vâng, tôi sốt 39 độ.", "Yes, 39 degrees.", null), WrongHint = Loc.T("Hôm nay bạn không sốt. Sốt thì không tiêm được.", "You have no fever today. With a fever you can't be vaccinated.", "") },
                new JobQuizCard.Choice { Label = "ごちそうさまでした。", Detail = Loc.T("Cảm ơn vì bữa ăn.", "Thanks for the meal.", null), WrongHint = Loc.T("Câu này nói sau bữa ăn.", "That's said after a meal.", "") },
            };
            JobQuizCard.Show(Kicker("予防接種"), Doctor, "きょう ねつは ありませんか。ちくっと しますよ。", "kyō netsu wa arimasen ka. chikutto shimasu yo",
                Loc.T("Hôm nay không sốt chứ? Sẽ hơi nhói một chút nhé.", "No fever today? This will sting a little.", ""), Loc.T("Bạn trả lời:", "You answer:", "なんと こたえますか。"), Shuffle(choices), () =>
                {
                    Current = Stage.Examined;
                    QuestService.Raise("clinic_vaccine", "clinic_doctor");
                    SocialStanding.AddMoodlet("vaccinated", SocialMood.Confident, 0.4f, 2f);
                    var flu = NihongoLife.Health.PlayerIllness.Catalog.Illness("flu");
                    if (flu != null) NihongoLife.Health.PlayerIllness.Immunize("flu", flu.immunityHours); // the flu shot protects for a season
                    Hint("「はい、おわりました。おだいじに。」 " + Loc.T("Xong rồi! Thanh toán ở quầy tiếp nhận.", "All done! Pay at reception.", "受付でお会計を。"), 6f);
                }, "clinic_doctor");
        }

        private static void HealthCheckResult()
        {
            if (!HealthCheckMeasured || EyeRounds < 3) { Hint(Loc.F("Còn: đo chiều cao {0} · đo thị lực {1}/3.", "Still to do: height {0} · eye test {1}/3.", "残り：身長 {0} · 視力 {1}/3", HealthCheckMeasured ? "✓" : "—", EyeRounds)); return; }
            QuestService.Raise("talk", DoctorId);
            Current = Stage.None;
            HealthCheckMeasured = false;
            EyeRounds = 0;
            Hint("「けっかは いじょう なしです。」 " + Loc.T("Kết quả: bình thường! Sức khoẻ tốt.", "Result: all normal! You're healthy.", "異常なし！"), 6f);
        }

        // ─────────── Paying (会計) and medicine (お薬) ───────────

        private static void Pay()
        {
            int fee = Catalog.visitFeeYen;
            var choices = new[] { fee, fee * 10, fee / 10, fee + 100 }.Distinct().Select(v => new JobQuizCard.Choice
            {
                Label = $"{v}えん", Detail = Loc.Yen(v), Correct = v == fee,
                WrongHint = Loc.F("Nghe lại: {0}えん = {1}.", "Listen again: {0}えん = {1}.", "", Kana(fee), Loc.Yen(fee)),
            }).ToList();
            JobQuizCard.Show(Kicker("会計"), Nurse, $"おかいけいは {Kana(fee)}えんです。", null, Catalog.FeeNote,
                Loc.T("Viện phí bao nhiêu?", "How much is the bill?", "いくらですか。"), Shuffle(choices), () =>
                {
                    var inventory = PlayerInventory.Instance;
                    bool paid = inventory != null && inventory.SpendYen(fee, Loc.Triple("Viện phí phòng khám", "Clinic fee", "診察料"));
                    if (!paid)
                    {
                        // Treatment never waits for money: the fee becomes an interest-free bill (bank app, due in 14 days).
                        NihongoLife.Bank.BankService.AddClinicBill(fee);
                        Hint(Loc.F("Không đủ tiền — chị Sato: 「こんどで いいですよ。」 Viện phí {0} ghi vào hoá đơn trả sau (không lãi, 14 ngày) — xem ở ví ¥ → Ngân hàng.", "Not enough money — Sato: 「こんどで いいですよ。」 The {0} fee is billed for later (no interest, 14 days) — see Wallet ¥ → Bank.", "「こんどで いいですよ。」 {0}は後払いになりました（14日以内・無利子）。", Loc.Yen(fee)), 8f);
                    }
                    QuestService.Raise("clinic_pay", "clinic_desk");
                    if (Reason == Purpose.Consult) { Current = Stage.Paid; Hint(Loc.T("Nhận thuốc ở quầy お薬.", "Collect your medicine at the お薬 window.", "お薬の窓口へどうぞ。")); }
                    else { Current = Stage.None; Hint("「おだいじに。」 " + Loc.T("Chúc mau khoẻ!", "Take care!", "")); }
                }, "clinic_fee");
        }

        private static void CollectMedicine()
        {
            if (Current != Stage.Paid || Symptom == null) { Hint(Loc.T("Chưa có đơn thuốc — khám bác sĩ và thanh toán trước.", "No prescription yet — see the doctor and pay first.", "まだ処方せんがありません。")); return; }
            var symptom = Symptom;
            var dosage = Catalog.Dosage(symptom.dosage);
            if (dosage == null) { Finish(); return; }
            var choices = Catalog.dosages.Where(d => d != dosage).Take(3).Append(dosage).Select(d => new JobQuizCard.Choice
            {
                Label = d.Meaning, Correct = d == dosage,
                WrongHint = Loc.F("Dược sĩ nói 「{0}」.", "The pharmacist said 「{0}」.", "「{0}」", dosage.ja),
            }).ToList();
            JobQuizCard.Show(Kicker("お薬"), Loc.T("Dược sĩ", "Pharmacist", "やくざいし"), $"{symptom.medicineJa}です。{dosage.ja}", null,
                Loc.F("Đây là {0}.", "This is {0}.", "", symptom.Medicine), Loc.T("Uống thuốc thế nào? Chọn đúng nghĩa:", "How do you take it? Pick the right meaning:", "どう のみますか。"), Shuffle(choices), Finish, "clinic_medicine");

            void Finish()
            {
                QuestService.Raise("clinic_medicine", "clinic_pharmacy");
                Current = Stage.None;
                Symptom = null;
                Hint("「おだいじに。」 " + Loc.T("Chúc mau khoẻ!", "Take care!", ""), 5f);
            }
        }

        // ─────────── Health check (健康診断) ───────────

        private static void MeasureHeight()
        {
            if (!Active(HealthCheckQuest) || Current != Stage.CheckedIn || Reason != Purpose.HealthCheck) { Hint(Loc.T("Đăng ký khám sức khoẻ ở quầy (受付) trước.", "Book a health check at reception (受付) first.", "先に受付で健康診断を。")); return; }
            if (HealthCheckMeasured) { Hint(Loc.T("Đã đo chiều cao ✓", "Height done ✓", "身長はOK ✓")); return; }
            var heights = Catalog.heights.Length > 0 ? Catalog.heights : new[] { 160 };
            int day = DayNightCycle.Active != null ? DayNightCycle.Active.Day : 0;
            int h = heights[day % heights.Length];
            var choices = heights.Where(x => x != h).Take(3).Append(h).Select(x => new JobQuizCard.Choice
            {
                Label = $"{x} cm", Correct = x == h,
                WrongHint = Loc.F("{0} là {1} cm.", "{0} is {1} cm.", "{0} = {1} cm", Kana(h), h),
            }).ToList();
            JobQuizCard.Show(Kicker("身長"), Nurse, $"しんちょうは {Kana(h)} センチです。", null, Loc.T("Chiều cao của bạn là…", "Your height is…", ""),
                Loc.T("Ghi số đúng vào phiếu:", "Write the right number:", "ただしい すうじは？"), Shuffle(choices), () =>
                {
                    HealthCheckMeasured = true;
                    QuestService.Raise("clinic_measure", "clinic_scale");
                    Hint(Loc.F("Chiều cao: {0} cm ✓", "Height: {0} cm ✓", "身長 {0} cm ✓", h));
                }, null);
        }

        private static readonly string[] Rings = { "⊂", "∪", "⊃", "∩" }; // gap right, up, left, down

        private static void EyeTest()
        {
            if (!Active(HealthCheckQuest) || Current != Stage.CheckedIn || Reason != Purpose.HealthCheck) { Hint(Loc.T("Đăng ký khám sức khoẻ ở quầy (受付) trước.", "Book a health check at reception (受付) first.", "先に受付で健康診断を。")); return; }
            if (EyeRounds >= 3) { Hint(Loc.T("Đã đo thị lực ✓ — gặp bác sĩ Mori.", "Eye test done ✓ — see Dr Mori.", "視力はOK ✓")); return; }
            var directions = Catalog.eyeDirections;
            if (directions.Length < 4) return;
            var target = directions[(EyeRounds * 3 + 1) % directions.Length];
            string ring = target.angle switch { 0 => Rings[0], 90 => Rings[1], 180 => Rings[2], _ => Rings[3] };
            var choices = directions.Select(d => new JobQuizCard.Choice
            {
                Label = d.ja, Detail = Loc.IsJapanese ? null : d.Meaning, Correct = d == target,
                WrongHint = Loc.T("Nhìn chỗ hở của vòng tròn.", "Look where the ring is open.", "わの あいて いる ところを みて。"),
            }).ToList();
            JobQuizCard.Show(Kicker("視力検査"), Nurse, $"<size=200%>{ring}</size>   どちらが あいて いますか。", "dochira ga aite imasu ka",
                Loc.T("Vòng tròn hở ở phía nào?", "Which side is the ring open?", ""), Loc.F("Lượt {0}/3:", "Round {0}/3:", "{0}/3：", EyeRounds + 1), choices, () =>
                {
                    EyeRounds++;
                    QuestService.Raise("clinic_eye", "clinic_eyechart");
                    if (EyeRounds >= 3) Hint(Loc.T("Thị lực 1.0 ✓ — gặp bác sĩ Mori để nghe kết quả.", "Eyesight 1.0 ✓ — see Dr Mori for the results.", "視力 1.0 ✓"));
                }, null);
        }

        // ─────────── Helpers ───────────

        public static string Kana(int yen)
        {
            if (yen <= 0) return "ゼロ";
            string[] ones = { "", "いち", "に", "さん", "よん", "ご", "ろく", "なな", "はち", "きゅう" };
            string[] hundreds = { "", "ひゃく", "にひゃく", "さんびゃく", "よんひゃく", "ごひゃく", "ろっぴゃく", "ななひゃく", "はっぴゃく", "きゅうひゃく" };
            string[] thousands = { "", "せん", "にせん", "さんぜん", "よんせん", "ごせん", "ろくせん", "ななせん", "はっせん", "きゅうせん" };
            int th = yen / 1000 % 10, h = yen / 100 % 10, t = yen / 10 % 10, o = yen % 10;
            string s = (yen >= 10000 ? (yen / 10000) + "まん" : "") + thousands[th] + hundreds[h];
            if (t > 0) s += (t == 1 ? "" : ones[t]) + "じゅう";
            return s + ones[o];
        }

        private static List<JobQuizCard.Choice> Shuffle(IEnumerable<JobQuizCard.Choice> choices) =>
            choices.OrderBy(c => (c.Label.GetHashCode() & 0x7fffffff) % 97).ToList();

        private static void Hint(string text, float seconds = 5f) => HudFeed.Post(text, HudFeed.Kind.Info, seconds, "clinic");
    }
}
