using System.Collections.Generic;
using System.Linq;
using NihongoLife.Core;
using NihongoLife.Fashion;
using NihongoLife.UI;

namespace NihongoLife.Progression
{
    /// <summary>
    /// The Hibari Mode shift (job_fashion_shift, also the Fashion career's work): tidy the clothes tables, answer
    /// customers about sizes and colours, look after the fitting room and ring up a sale, in Japanese.
    /// Content: Resources/Fashion/fashion.json.
    /// </summary>
    public sealed partial class JobStation
    {
        private static string FashionKicker(string place) => Loc.T("LÀM Ở HIBARI MODE", "HIBARI MODE SHIFT", "ひばりモード勤務") + " · " + place;

        private void FashionFold()
        {
            TimedAction.Run(Loc.T("Đang gấp quần áo…", "Folding the clothes…", "服をたたんでいます…"), Loc.T("tay", "by hand", "手で"), 2.2f, () =>
            {
                JobShift.MarkDone(stationId);
                QuestService.Raise("fold", "fashion_table");
            }, gesture: "fold_clothes");
        }

        private void FashionQuiz(FashionRequest[] list, string place, System.Action done)
        {
            if (list.Length == 0) return;
            var request = list[ShiftSeed % list.Length];
            var choices = new List<JobQuizCard.Choice>
            {
                new JobQuizCard.Choice { Label = request.answerJa, Detail = Loc.Pick(request.answerVi, request.answerEn, null), Correct = true },
            };
            for (int i = 0; i < request.wrongJa.Length; i++)
                choices.Add(new JobQuizCard.Choice
                {
                    Label = request.wrongJa[i],
                    Detail = Loc.Pick(request.wrongVi[i], request.wrongEn[i], null),
                    WrongHint = Loc.T("Câu này không trả lời điều khách hỏi. Nghe lại nhé.", "That does not answer the customer. Listen again.", "お客さまの質問の答えではありません。"),
                });
            JobQuizCard.Show(FashionKicker(place), Loc.T("Khách hàng", "Customer", "おきゃくさま"), request.lineJa, request.reading, Loc.Pick(request.vi, request.en, ""),
                Loc.T("Bạn trả lời thế nào?", "What do you answer?", "なんと こたえますか。"), choices.OrderBy(c => (c.Label.GetHashCode() & 0x7fffffff) % 11).ToList(), done, "checkroom");
        }

        private void FashionAdvise() => FashionQuiz(FashionCatalog.Floor.requests, "売り場", () =>
        {
            JobShift.MarkDone(stationId);
            QuestService.Raise("advise", "fashion_customer");
            HudFeed.Post("「ありがとうございます！」" + Loc.T(" — Khách tìm được món mình cần.", " — The customer found what they wanted.", ""), HudFeed.Kind.Info, 3f);
        });

        private void FashionFitting() => FashionQuiz(FashionCatalog.Floor.fittings, "試着室", () =>
        {
            QuestService.Raise("fit", "fashion_fitting");
            HudFeed.Post("「これに します！」" + Loc.T(" — Khách ưng bộ này.", " — The customer takes this one.", ""), HudFeed.Kind.Info, 3f);
        });

        private void FashionCheckout()
        {
            var outfits = FashionCatalog.Load().outfits;
            if (outfits.Length < 2) return;
            int seed = ShiftSeed;
            var a = outfits[seed % outfits.Length];
            var b = outfits[(seed / 7 + 3) % outfits.Length];
            if (a == b) b = outfits[(System.Array.IndexOf(outfits, a) + 1) % outfits.Length];
            int total = a.price + b.price;
            var options = new[] { total, total + 1000, total - 500, total + 5000 }.Where(v => v > 0).Distinct().OrderBy(v => (v * 37) % 11).ToList();
            var choices = options.Select(v => new JobQuizCard.Choice
            {
                Label = $"{v}えん",
                Detail = Loc.Yen(v),
                Correct = v == total,
                WrongHint = Loc.F("Cộng lại nhé: ¥{0} + ¥{1}.", "Add it up again: ¥{0} + ¥{1}.", "もう一度たしましょう：{0}円 + {1}円", a.price, b.price),
            }).ToList();
            JobQuizCard.Show(FashionKicker("レジ"), Loc.T("Khách hàng", "Customer", "おきゃくさま"), "これと これを おねがいします。", "kore to kore o onegai shimasu",
                Loc.T("Cho tôi hai món này.", "These two, please.", ""),
                Loc.F("Trên máy: ¥{0} và ¥{1}. Tổng bao nhiêu?", "On the till: ¥{0} and ¥{1}. What is the total?", "レジ：{0}円と{1}円。ごうけいは いくらですか。", a.price, b.price), choices, () =>
                {
                    QuestService.Raise("checkout", "fashion_register");
                    HudFeed.Post($"「{total}えん です。ありがとうございました！」", HudFeed.Kind.Info, 3f);
                });
        }
    }
}
