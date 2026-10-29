import sys; sys.path.insert(0,r'D:\VTC_Academy\_grok_tmp\tools2')
from pt import *
W=r'Scripts\UI\WorldMapUI.cs'
patch(W,'        private Button _areaTab, _overviewTab;','        private Button _areaTab, _overviewTab, _guideButton;\n        private TextMeshProUGUI _sidebarTitle;')
patch(W,'''            var guide = NLUi.Button(window, "GuideButton", NihongoLife.Core.Loc.T("Sổ tay · địa điểm", "Handbook · places", "ハンドブック・場所"), _font,''','''            var guide = NLUi.Button(window, "GuideButton", GuideLabel(), _font,''')
patch(W,'''            NLUi.Anchor((RectTransform)guide.transform, new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(230f, 44f));''','''            _guideButton = guide;
            NLUi.Anchor((RectTransform)guide.transform, new Vector2(1f, 0f), new Vector2(-30f, 22f), new Vector2(230f, 44f));''')
patch(W,'''            NLUi.Label(sidebar, "Title", "ちめい  <size=75%><color=#A8B4C4>" + NihongoLife.Core.Loc.T("Địa điểm · bấm để đi tới", "Places · click to travel", "場所・クリックで移動") + "</color></size>", 21f, NLUi.Text, _font, FontStyles.Bold);''','''            _sidebarTitle = NLUi.Label(sidebar, "Title", SidebarTitle(), 21f, NLUi.Text, _font, FontStyles.Bold);''')
patch(W,'''        private void HandleGlobalLanguageChanged(GameLanguage _)
        {''','''        private static string GuideLabel() => NihongoLife.Core.Loc.T("Sổ tay · địa điểm", "Handbook · places", "ハンドブック・場所");

        private static string SidebarTitle() => "ちめい  <size=75%><color=#A8B4C4>" + NihongoLife.Core.Loc.T("Địa điểm · bấm để đi tới", "Places · click to travel", "場所・クリックで移動") + "</color></size>";

        private void HandleGlobalLanguageChanged(GameLanguage _)
        {
            if (_guideButton != null) _guideButton.GetComponentInChildren<TextMeshProUGUI>().text = GuideLabel();
            if (_sidebarTitle != null) _sidebarTitle.text = SidebarTitle();''')