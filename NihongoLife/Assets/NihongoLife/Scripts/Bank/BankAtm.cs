using NihongoLife.Interaction;
using UnityEngine;

namespace NihongoLife.Bank
{
    /// <summary>A Hibari Shinkin ATM (konbini, station hall): F opens the bank (BankUI) — loans, repayments, the clinic bill.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BankAtm : MonoBehaviour, IInteractable
    {
        public string GetPromptJa() => "ATMを つかう";
        public string GetpromptEn() => NihongoLife.Core.Loc.T("Dùng ATM (ngân hàng)", "Use the ATM (bank)", "ATMを使う");
        public Transform GetTransform() => transform;
        public void Interact(GameObject player) => NihongoLife.UI.BankUI.Open();
    }
}
