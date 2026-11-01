using NihongoLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    sealed class TouchControlSettingsBinder : MonoBehaviour
    {
        TextMeshProUGUI _title;
        TextMeshProUGUI _mode;
        TextMeshProUGUI _sizeLabel;
        TextMeshProUGUI _opacityLabel;
        TextMeshProUGUI _hand;
        TextMeshProUGUI _hint;
        Slider _size;
        Slider _opacity;

        public void Bind(TextMeshProUGUI title, Button mode, TextMeshProUGUI sizeLabel, Slider size, TextMeshProUGUI opacityLabel, Slider opacity, Button hand, TextMeshProUGUI hint)
        {
            _title = title;
            _mode = mode.GetComponentInChildren<TextMeshProUGUI>();
            _sizeLabel = sizeLabel;
            _opacityLabel = opacityLabel;
            _hand = hand.GetComponentInChildren<TextMeshProUGUI>();
            _hint = hint;
            _size = size;
            _opacity = opacity;
            mode.onClick.AddListener(() => TouchControlPrefs.CycleMode());
            hand.onClick.AddListener(() => TouchControlPrefs.LeftHanded = !TouchControlPrefs.LeftHanded);
            size.onValueChanged.AddListener(value =>
            {
                if (Mathf.Abs(value - TouchControlPrefs.Size) < 0.001f) return;
                TouchControlPrefs.Size = value;
            });
            opacity.onValueChanged.AddListener(value =>
            {
                if (Mathf.Abs(value - TouchControlPrefs.Opacity) < 0.001f) return;
                TouchControlPrefs.Opacity = value;
            });
            Refresh();
        }

        private void OnEnable()
        {
            TouchControlPrefs.Changed += Refresh;
            Loc.Watch(this, Refresh);
            Refresh();
        }

        private void OnDisable() => TouchControlPrefs.Changed -= Refresh;

        void Refresh()
        {
            if (_title != null) _title.text = Loc.T("Điều khiển cảm ứng", "Touch controls", "タッチ操作");
            if (_mode != null) _mode.text = Loc.T(TouchControlPrefs.ModeVi(), TouchControlPrefs.ModeEn(), TouchControlPrefs.ModeJa());
            if (_hand != null) _hand.text = Loc.T(TouchControlPrefs.HandVi(), TouchControlPrefs.HandEn(), TouchControlPrefs.HandJa());
            if (_sizeLabel != null) _sizeLabel.text = Loc.F("Cỡ nút  {0}", "Button size  {0}", "ボタンサイズ  {0}", TouchControlPrefs.Size.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            if (_opacityLabel != null) _opacityLabel.text = Loc.F("Độ mờ  {0}", "Opacity  {0}", "不透明度  {0}", TouchControlPrefs.Opacity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            if (_hint != null) _hint.text = Loc.T(
                "Tự động hiện trên điện thoại và máy tính bảng. Bàn phím và chuột vẫn dùng được. Ẩn nhanh bằng nút Ẩn trên màn hình.",
                "Auto appears on phones and tablets. Keyboard and mouse keep working. Hide hides them until you tap Controls.",
                "スマホとタブレットでは自動で表示。キーボードとマウスはそのまま使えます。隠すで一時的に消せます。");
            if (_size != null) _size.SetValueWithoutNotify(TouchControlPrefs.Size);
            if (_opacity != null) _opacity.SetValueWithoutNotify(TouchControlPrefs.Opacity);
        }
    }
}
