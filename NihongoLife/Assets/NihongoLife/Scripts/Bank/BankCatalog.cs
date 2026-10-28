using System;
using NihongoLife.Core;
using UnityEngine;

namespace NihongoLife.Bank
{
    /// <summary>One kind of loan (Resources/Bank/bank.json "products").</summary>
    [Serializable]
    public sealed class LoanProduct
    {
        public string id, vi, en, ja, noteVi, noteEn, noteJa;
        public int minLevel = 1;
        public int maxYen;
        /// <summary>Added to the limit per player level (0 = a fixed limit).</summary>
        public int perLevelYen;
        /// <summary>A flat fee on the amount borrowed, in percent.</summary>
        public int interestPercent;
        public int termDays = 14;
        /// <summary>Only offered while the wallet holds less than this (0 = always).</summary>
        public int onlyBelowYen;
        /// <summary>Refused while the credit score is under creditMinForCard.</summary>
        public bool needsCredit;

        public string Name => Loc.T(vi, en, ja);
        public string Note => Loc.T(noteVi, noteEn, noteJa);
    }

    /// <summary>
    /// The bank's rules (Resources/Bank/bank.json): products, the credit score, and what happens when a loan is late.
    /// New products and rules are JSON only; BankCatalogTests validates the file.
    /// </summary>
    [Serializable]
    public sealed class BankCatalog
    {
        public string schema, nameVi, nameEn, nameJa;
        public int creditStart = 60, creditMax = 100, creditOnTimeRepay = 8, creditLate = 12, creditMinForCard = 30;
        public int graceDays = 3, remindDaysBefore = 2;
        public int lateFeePercent = 2, lateFeeEveryDays = 7, lateFeeCapPercent = 10;
        /// <summary>Share of each income kept back for an overdue loan; the rest stays with the player.</summary>
        public int garnishPercent = 30;
        public int extensionDays = 14;
        public LoanProduct[] products = Array.Empty<LoanProduct>();

        public string Name => Loc.T(nameVi, nameEn, nameJa);

        private static BankCatalog _cached;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _cached = null;

        public static BankCatalog Load()
        {
            if (_cached != null) return _cached;
            var text = Resources.Load<TextAsset>("Bank/bank");
            _cached = text != null ? JsonUtility.FromJson<BankCatalog>(text.text) : new BankCatalog();
            _cached.products ??= Array.Empty<LoanProduct>();
            return _cached;
        }

        public LoanProduct Product(string id) => Array.Find(products, p => p.id == id);
    }
}
