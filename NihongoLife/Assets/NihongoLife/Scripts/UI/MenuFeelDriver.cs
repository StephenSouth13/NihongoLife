using System.Collections.Generic;
using NihongoLife.Audio;
using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Finds every menu button, including ones created when a popup opens, and gives it <see cref="MenuButtonFeel"/>.</summary>
    public sealed class MenuFeelDriver : MonoBehaviour
    {
        readonly HashSet<int> _dressed = new HashSet<int>();
        readonly HashSet<int> _entered = new HashSet<int>();

        private float _nextScan;

        private void LateUpdate()
        {
            // Buttons are added by the menu builders as it opens; a few scans a second dress them without walking the
            // whole hierarchy every frame.
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.2f;
            int order = 0;
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                int id = button.GetInstanceID();
                if (_dressed.Add(id) && button.GetComponent<MenuButtonFeel>() == null)
                    button.gameObject.AddComponent<MenuButtonFeel>();
                if (!button.gameObject.activeInHierarchy || !_entered.Add(id)) continue;
                var feel = button.GetComponent<MenuButtonFeel>();
                if (feel != null) feel.PlayEntrance(order * 0.045f);
                order++;
            }
        }
    }
}
