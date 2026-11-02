using System;
using System.Collections;
using NihongoLife.Core;
using NihongoLife.Player;
using NihongoLife.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NihongoLife.Tests
{
    /// <summary>The company runs on the calendar by itself, and work outside the chosen field counts for half.</summary>
    public class BusinessLedgerPlayModeTests
    {
        private BusinessSystem _sys;

        [TearDown]
        public void TearDown()
        {
            BusinessWorkers.Today = () => DayNightCycle.Active != null ? DayNightCycle.Active.Day : 0;
            if (_sys != null && _sys.Business != null)
            {
                var b = _sys.Business;
                b.companyName = null; b.capitalYen = 0; b.employeeCount = 0; b.workers.Clear(); b.lastSettledDay = b.weekStartDay = -1;
                b.weekIncomeYen = b.weekWagesYen = b.unpaidCostYen = 0;
                _sys.Persist();
            }
            var state = CareerService.State;
            if (state != null) state.majorIndustry = string.Empty;
        }

        private IEnumerator Town()
        {
            yield return SceneManager.LoadSceneAsync(WorldLocationCatalog.CityScene);
            for (float t = 0f; t < 25f && BusinessSystem.Instance == null; t += Time.unscaledDeltaTime) yield return null;
            _sys = BusinessSystem.Instance;
            Assert.NotNull(_sys);
        }

        [UnityTest]
        public IEnumerator Clinic_StaffEarnDaily_AndTheWeekClosesAlone()
        {
            yield return Town();
            int day = 500;
            BusinessWorkers.Today = () => day;
            var b = _sys.Business;
            b.companyName = "Test Clinic"; b.kind = "clinic"; b.capitalYen = 100000; b.reputation = 10; b.employeeCount = 0; b.workers.Clear();
            b.lastSettledDay = b.weekStartDay = day; b.weekIncomeYen = b.weekWagesYen = b.unpaidCostYen = 0;
            var cook = BusinessWorkers.Hire(_sys, "cook", 26000);
            Assert.NotNull(cook);
            int capital = b.capitalYen;
            float clinic = BusinessLedger.Kind("clinic").revenueFactor;

            day++;
            BusinessLedger.Tick(_sys, day);
            int gross = Mathf.FloorToInt(220 * clinic * BusinessWorkers.HoursPerDay);
            Assert.AreEqual(gross, b.weekIncomeYen, 1, "A day's work counts without a button");
            Assert.Greater(b.capitalYen, capital, "The company earned by itself");

            int wallet = PlayerInventory.Instance.Yen;
            capital = b.capitalYen;
            day = b.weekStartDay + 7;
            BusinessLedger.Tick(_sys, day);
            yield return null;
            Assert.AreEqual(500 + 7, b.weekStartDay, "A new week began");
            Assert.AreEqual(0, b.weekIncomeYen);
            Assert.Greater(PlayerInventory.Instance.Yen, wallet, "Part of the profit reached the wallet");

            // A week the company cannot pay: the wallet is not touched and the debt waits.
            b.capitalYen = 0; b.workers.Clear(); b.employeeCount = 0;
            wallet = PlayerInventory.Instance.Yen;
            int reputation = b.reputation;
            day += 7;
            BusinessLedger.Tick(_sys, day);
            Assert.AreEqual(wallet, PlayerInventory.Instance.Yen, "Costs never come out of the wallet");
            Assert.AreEqual(BusinessLedger.Kind("clinic").WeeklyCost, b.unpaidCostYen);
            Assert.Less(b.reputation, reputation);
        }

        [UnityTest]
        public IEnumerator Work_OutsideTheChosenField_PaysHalf()
        {
            yield return Town();
            var state = CareerService.State;
            Assert.NotNull(state);
            state.majorIndustry = "foodservice";
            Assert.AreEqual(1f, CareerService.RewardFactor("chef"), "Inside the field: full pay");
            Assert.AreEqual(0.5f, CareerService.RewardFactor("medical"), "Outside the field: half");
            Assert.AreEqual(0.5f, CareerService.RewardFactor(CareerService.CareerOf(ProgressionCatalog.Load().Quest("job_clinic_shift"))), "A shift counts for its career");
            state.majorIndustry = string.Empty;
            Assert.AreEqual(1f, CareerService.RewardFactor("medical"), "No field chosen yet: full pay");
        }
    }
}
