using System;
using System.Collections.Generic;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Shows / hides / destroys a card's shadow together with the card.</summary>
    public sealed class UiShadowLink : MonoBehaviour
    {
        public GameObject Shadow;
        private void OnEnable() { if (Shadow != null) Shadow.SetActive(true); }
        private void OnDisable() { if (Shadow != null) Shadow.SetActive(false); }
        private void OnDestroy() { if (Shadow != null) Destroy(Shadow); }
    }
}
