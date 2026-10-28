using System.Collections;
using System.IO;
using System.Linq;
using NihongoLife.Bank;
using NihongoLife.Core;
using NihongoLife.Dialogue;
using NihongoLife.Player;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>
    /// The bank in the real town, and the promise behind it: a broke, hungry player always has a way out, and a late
    /// loan is firm without ever taking everything.
    /// · broke → the interest-free emergency fund opens and pays into the wallet; with money it is closed;
    /// · repaying on time clears the loan and raises the credit score;
    /// · overdue → a capped late fee, credit down, only part of each income is kept back, the wallet never below 0;
    /// · one free extension clears the overdue state; an unpaid clinic fee becomes an interest-free bill;
    /// · a save written while starving does not faint the player on load.
    /// Captures: Bao_Cao/bank.
    /// </summary>
    public class BankPlayModeTests
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Bao_Cao/bank"));

        [UnityTest]
        public IEnumerator Bank_SafetyNetLoansAndGentleLateRules()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            yield return new WaitForSecondsRealtime(2.5f);
            var dm = DialogueManager.Instance;
            while (dm != null && dm.IsOpen) { dm.CancelDialogue(); if (dm.IsOpen) dm.ContinueDialogue(); yield return null; }
            var inventory = PlayerInventory.Instance;
            var record = BankService.Record;
            Assert.NotNull(record, "The save has a bank record");
            record.loans.Clear();
            record.credit = BankService.Catalog.creditStart;
            var c = BankService.Catalog;
            var emergency = c.Product("emergency");
            var card = c.Product("card");

            // With money in the wallet the emergency fund is closed.
            if (inventory.Yen < emergency.onlyBelowYen) inventory.AddYen(emergency.onlyBelowYen + 500);
            Assert.IsNotNull(BankService.Blocker(emergency), "Not for a player with money");
            BankUI.Open();
            yield return null;
            Capture("01_bank_with_money");

            // Broke: the emergency fund lends without interest.
            inventory.ApplyFine(inventory.Yen);
            Assert.AreEqual(0, inventory.Yen);
            Assert.IsNull(BankService.Blocker(emergency), "Open to a broke player at level 1");
            Assert.IsNull(BankService.Borrow("emergency", 2000));
            Assert.AreEqual(2000, inventory.Yen, "The money is in the wallet");
            var loan = BankService.Loans.Single();
            Assert.AreEqual(2000, loan.owed, "No interest");
            Assert.AreEqual(BankService.Today + emergency.termDays, loan.dueDay);
            Assert.IsNotNull(BankService.Blocker(emergency), "Only one emergency loan at a time");
            yield return null;
            Capture("02_emergency_loan");

            // Repaid on time: cleared, credit up.
            int credit = BankService.Credit;
            Assert.IsNull(BankService.Repay(loan.id, loan.owed));
            Assert.AreEqual(0, BankService.Loans.Count);
            Assert.AreEqual(credit + c.creditOnTimeRepay, BankService.Credit, "On-time repayment raises credit");

            // A personal loan, then it goes overdue.
            PlayerStatus.Instance.AddExp(5000);
            Assert.IsNull(BankService.Blocker(card), "Personal loan open from level " + card.minLevel);
            int limit = BankService.Limit(card);
            Assert.Greater(limit, 0);
            Assert.IsNull(BankService.Borrow("card", Mathf.Min(limit, 3000)));
            loan = BankService.Loans.Single();
            Assert.AreEqual(loan.principal + Mathf.CeilToInt(loan.principal * card.interestPercent / 100f), loan.owed, "Flat fee");
            int today = BankService.Today;
            loan.dueDay = today - c.graceDays - 1; // overdue now
            credit = BankService.Credit;
            int owedBefore = loan.owed;
            BankService.Tick(today);
            Assert.IsTrue(BankService.IsOverdue(loan, today));
            Assert.AreEqual(credit - c.creditLate, BankService.Credit, "Credit down once");
            Assert.Greater(loan.owed, owedBefore, "A late fee");
            BankService.Tick(today);
            BankService.Tick(today + 100);
            Assert.LessOrEqual(loan.lateFees, Mathf.CeilToInt(loan.principal * c.lateFeeCapPercent / 100f), "Late fees are capped");
            Assert.IsNotNull(BankService.Blocker(card), "No new personal loan while overdue");

            // Income while overdue: only garnishPercent is kept back, the rest stays.
            inventory.ApplyFine(inventory.Yen);
            Assert.IsNull(BankService.Blocker(emergency), "Overdue or not, food money is still there for a broke player");
            int owed = loan.owed;
            inventory.AddYen(1000, Loc.Triple("Lương", "Salary", "給料"));
            int kept = Mathf.FloorToInt(1000 * c.garnishPercent / 100f);
            Assert.AreEqual(1000 - kept, inventory.Yen, "The player keeps most of the income");
            Assert.AreEqual(owed - kept, loan.owed, "The kept-back part repays the loan");
            Assert.GreaterOrEqual(inventory.Yen, 0);
            yield return null;
            BankUI.Open();
            yield return null;
            Capture("03_overdue");

            // One free extension clears the overdue state; a second is refused.
            Assert.IsNull(BankService.Extend(loan.id));
            Assert.IsFalse(BankService.IsOverdue(loan, BankService.Today));
            Assert.IsNotNull(BankService.Extend(loan.id), "Only once");
            BankUI.Close();

            // An unpaid clinic fee: an interest-free bill.
            record.loans.Clear();
            BankService.AddClinicBill(1500);
            var bill = BankService.Loans.Single();
            Assert.AreEqual(BankService.ClinicBill, bill.productId);
            Assert.AreEqual(1500, bill.owed);
            bill.dueDay = BankService.Today - 30;
            BankService.Tick(BankService.Today);
            Assert.AreEqual(1500, bill.owed, "The clinic charges no late fee");
            record.loans.Clear();

            // A save written while starving does not faint the player on load.
            var status = PlayerStatus.Instance;
            status.DebugSetNeeds(health: 0f, hunger: 0f, thirst: 0f, sleepiness: 100f);
            if (GameServices.TryGet(out NihongoLife.Save.IProgressRepository repository))
            {
                var progress = repository.GetProgress();
                progress.health = 0f; progress.hunger = 0f; progress.thirst = 0f; progress.sleepiness = 100f;
                status.ReloadProgress();
                Assert.Greater(status.CurrentHealth, 0f, "Awake after loading");
                Assert.GreaterOrEqual(status.Hunger, PlayerStatus.HealthyNeed, "Fed enough to heal");
                Assert.GreaterOrEqual(status.Thirst, PlayerStatus.HealthyNeed);
                Assert.Less(status.Sleepiness, 100f, "Not nodding off");
            }
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsFalse(Object.FindFirstObjectByType<StatusConsequences>() is { IsBusy: true }, "No blackout right after loading");
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            if (camera == null) return;
            const int width = 1600, height = 900;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy).OrderBy(c => c.sortingOrder).ToArray();
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceCamera; canvases[i].worldCamera = camera; canvases[i].planeDistance = camera.nearClipPlane + 0.06f + 0.002f * (canvases.Length - i); }
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            Canvas.ForceUpdateCanvases(); // back to screen size now, or HudFitRect fits the next frame to the camera-mode size
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
