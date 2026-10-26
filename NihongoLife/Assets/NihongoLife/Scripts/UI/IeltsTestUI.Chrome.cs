using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NihongoLife.Core;
using NihongoLife.Exam.Ielts;
using NihongoLife.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NihongoLife.UI
{
    /// <summary>Partial of IeltsTestUI: screen chrome, responsive layout, tabs, and navigator. The MonoBehaviour entry point stays IeltsTestUI.cs.</summary>
    public sealed partial class IeltsTestUI : MonoBehaviour
    {
        // ─────────── Building the screen ───────────

        private void Build()
        {
            _canvas = NLUi.CreateCanvas("IeltsTestCanvas", 760);
            _root = new GameObject("IeltsRoot", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            _root.SetParent(_canvas.transform, false);
            NLUi.Stretch(_root);
            _root.GetComponent<Image>().color = Bg;

            // Header: title · mode chip · highlight tools · timer pill · close
            _header = Strip(_root, "Header", 0f, 70f, top: true, UiTheme.Ink);
            var line = UiKit.Image(_header, "Line", UiTheme.Line);
            UiKit.Strip(line.rectTransform, false, 0f, 1f);
            _title = Text(_header, "Title", "", 21f, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(_title.rectTransform, 28f, 0f, 1000f, 70f, Vector2.zero);
            _title.overflowMode = TextOverflowModes.Ellipsis;
            _modeChipBg = UiKit.Image(_header, "ModeBg", UiKit.Alpha(UiTheme.Teal, 0.2f), UiKit.Rounded(16));
            _modeChip = Text((RectTransform)_modeChipBg.transform, "Mode", "", 13.5f, UiTheme.Teal, TextAlignmentOptions.Center, FontStyles.Bold);
            UiKit.Fill(_modeChip.rectTransform);
            _timerBg = UiKit.Image(_header, "TimerBg", UiTheme.Sunken, UiKit.Rounded(20));
            _timer = Text((RectTransform)_timerBg.transform, "Timer", "", 16f, new Color(0.88f, 0.92f, 0.97f), TextAlignmentOptions.Center);
            UiKit.Fill(_timer.rectTransform, 14f, 0f, 14f, 0f);
            _timer.overflowMode = TextOverflowModes.Ellipsis;
            UiKit.CloseButton(_header, RequestClose, 13f);
            _toolbar = HighlightToolbar.Create(_header, _font, ClearHighlights, CopyHighlightsToNotebook, 40f);
            _highlights.Changed += () => { if (_attempt != null && !IsSubmitted) _saveAt = Time.unscaledTime + 0.6f; };

            // Audio bar: part tabs · play · progress · time · rules note
            _audioBar = Strip(_root, "AudioBar", 70f, 74f, top: true, new Color(0.075f, 0.098f, 0.135f, 1f));
            _tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            _tabs.SetParent(_audioBar, false);
            Place(_tabs, 28f, 14f, 520f, 46f, Vector2.zero);
            var tabLayout = _tabs.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 8f; tabLayout.childForceExpandWidth = false; tabLayout.childControlWidth = true; tabLayout.childControlHeight = true;
            _playButton = UiKit.Button(_audioBar, "Play", null, null, null, TogglePlay, UiKit.Kind.Accent, "play_arrow", 16f, 23);
            _playLabel = UiKit.LabelOf(_playButton);
            Place((RectTransform)_playButton.transform, 570f, 14f, 170f, 46f, Vector2.zero);
            var track = UiKit.Image(_audioBar, "Progress", new Color(1f, 1f, 1f, 0.12f), UiKit.Rounded(6), true);
            _progressTrack = track.rectTransform;
            Place(_progressTrack, 760f, 31f, 520f, 12f, Vector2.zero);
            var trigger = _progressTrack.gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(OnTrackClicked);
            trigger.triggers.Add(entry);
            var fill = UiKit.Image(_progressTrack, "Fill", UiTheme.Gold, UiKit.Rounded(6));
            _progressFill = fill.rectTransform;
            _progressFill.anchorMin = Vector2.zero; _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _audioTime = Text(_audioBar, "Time", "00:00 / 00:00", 16f, Color.white, TextAlignmentOptions.MidlineLeft);
            Place(_audioTime.rectTransform, 1296f, 14f, 150f, 46f, Vector2.zero);
            _audioStatus = Text(_audioBar, "Status", "", 14f, NLUi.Muted, TextAlignmentOptions.MidlineLeft);
            Place(_audioStatus.rectTransform, 760f, 44f, 520f, 26f, Vector2.zero);
            _note = Text(_audioBar, "Note", "", 13.5f, NLUi.Soft, TextAlignmentOptions.MidlineRight);
            _note.textWrappingMode = TextWrappingModes.Normal;
            Place(_note.rectTransform, -28f, 6f, 430f, 62f, new Vector2(1f, 0f));

            // Body (questions; passage column added for reading parts)
            _bodyArea = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
            _bodyArea.SetParent(_root, false);
            _bodyArea.anchorMin = Vector2.zero; _bodyArea.anchorMax = Vector2.one;
            _bodyArea.offsetMin = new Vector2(0f, 92f); _bodyArea.offsetMax = new Vector2(0f, -144f);
            _passageScroll = MakeScroll(_bodyArea, "PassageScroll", out _passageContent);
            _questionScroll = MakeScroll(_bodyArea, "QuestionScroll", out _questionContent);

            // Footer: question navigator · previous / next part · submit
            _footer = Strip(_root, "Footer", 0f, 92f, top: false, UiTheme.Ink);
            var topLine = UiKit.Image(_footer, "Line", UiTheme.Line);
            UiKit.Strip(topLine.rectTransform, true, 0f, 1f);
            _navigator = new GameObject("Navigator", typeof(RectTransform), typeof(GridLayoutGroup)).GetComponent<RectTransform>();
            _navigator.SetParent(_footer, false);
            Place(_navigator, 28f, 14f, 1220f, 64f, Vector2.zero);
            var grid = _navigator.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(28f, 28f); grid.spacing = new Vector2(4f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedRowCount; grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _prevButton = UiKit.Button(_footer, "Prev", "Phần trước", "Previous part", "前のパート", () => ShowPart(_viewPart - 1), UiKit.Kind.Ghost, "chevron_left", 15f);
            Place((RectTransform)_prevButton.transform, -470f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _nextButton = UiKit.Button(_footer, "Next", "Phần sau", "Next part", "次のパート", () => ShowPart(_viewPart + 1), UiKit.Kind.Secondary, "chevron_right", 15f);
            Place((RectTransform)_nextButton.transform, -310f, 21f, 150f, 50f, new Vector2(1f, 0f));
            _submitButton = UiKit.Button(_footer, "Submit", "Nộp bài", "Submit", "提出", RequestSubmit, UiKit.Kind.Primary, "task_alt", 17f);
            Place((RectTransform)_submitButton.transform, -28f, 21f, 260f, 50f, new Vector2(1f, 0f));

            BuildResults();
            BuildConfirm();
            ApplyResponsiveLayout(force: true);
            _root.gameObject.SetActive(false);
            Loc.Watch(this, () =>
            {
                if (!IsOpen || _test == null) return;
                RefreshChrome();
                RefreshAudioBar();
                if (_results.gameObject.activeSelf) { if (IsProductive) ShowProductiveResults(); else if (LastResult != null) ShowResults(); }
                else ShowPart(_viewPart);
            });
        }

        /// <summary>Keeps the header, audio controls and footer readable on 4:3 and narrow game views.</summary>
        private void ApplyResponsiveLayout(bool force = false)
        {
            if (_root == null || _header == null) return;
            float width = _root.rect.width;
            if (width <= 0f || (!force && Mathf.Abs(width - _layoutWidth) < 0.5f)) return;
            _layoutWidth = width;

            bool compact = width < 1540f;
            float closeReserve = 74f;
            float timerWidth = compact ? 330f : 410f;
            float modeWidth = compact ? 116f : 140f;
            Place((RectTransform)_timerBg.transform, -closeReserve, 13f, timerWidth, 44f, new Vector2(1f, 0f));
            Place((RectTransform)_modeChipBg.transform, -(closeReserve + timerWidth + 12f), 19f, modeWidth, 32f, new Vector2(1f, 0f));
            HighlightToolbar.SetCompact(_toolbar, compact);
            float toolbarWidth = LayoutUtility.GetPreferredWidth(_toolbar);
            Place(_toolbar, -(closeReserve + timerWidth + modeWidth + 24f), 15f, toolbarWidth, 40f, new Vector2(1f, 0f));
            float titleRight = closeReserve + timerWidth + modeWidth + toolbarWidth + 48f;
            Place(_title.rectTransform, 28f, 0f, Mathf.Max(200f, width - titleRight - 28f), 70f, Vector2.zero);

            float tabsWidth = Mathf.Clamp(width * (compact ? 0.34f : 0.28f), 360f, 520f);
            Place(_tabs, 28f, 14f, tabsWidth, 46f, Vector2.zero);
            float playX = 28f + tabsWidth + 16f;
            float noteWidth = compact ? 300f : 410f;
            float playWidth = compact ? 138f : 170f;
            Place((RectTransform)_playButton.transform, playX, 14f, playWidth, 46f, Vector2.zero);
            float progressX = playX + playWidth + 18f;
            float audioTimeWidth = compact ? 126f : 150f;
            float noteLeft = width - noteWidth - 28f;
            float progressWidth = Mathf.Max(150f, noteLeft - audioTimeWidth - 24f - progressX);
            Place(_progressTrack, progressX, 31f, progressWidth, 12f, Vector2.zero);
            Place(_audioStatus.rectTransform, progressX, 44f, progressWidth, 26f, Vector2.zero);
            Place(_audioTime.rectTransform, progressX + progressWidth + 12f, 14f, audioTimeWidth, 46f, Vector2.zero);
            Place(_note.rectTransform, -28f, 6f, noteWidth, 62f, new Vector2(1f, 0f));
            _audioStatus.gameObject.SetActive(_test != null && IsListening && !compact);

            float submitWidth = compact ? 190f : 230f;
            float navWidth = Mathf.Max(300f, width - (compact ? 560f : 620f));
            Place(_navigator, 28f, 14f, navWidth, 64f, Vector2.zero);
            Place((RectTransform)_submitButton.transform, -28f, 21f, submitWidth, 50f, new Vector2(1f, 0f));
            Place((RectTransform)_nextButton.transform, -(40f + submitWidth), 21f, 140f, 50f, new Vector2(1f, 0f));
            Place((RectTransform)_prevButton.transform, -(192f + submitWidth), 21f, 140f, 50f, new Vector2(1f, 0f));
        }

        private void BuildTabs()
        {
            var tabs = _root.Find("AudioBar/Tabs");
            foreach (Transform child in tabs) Destroy(child.gameObject);
            _partTabs.Clear();
            for (int i = 0; i < _test.parts.Length; i++)
            {
                int index = i;
                var part = _test.parts[i];
                var groups = part.groups ?? Array.Empty<IeltsGroup>();
                string range = groups.Length > 0 ? $"<size=75%>  {groups.Min(g => g.from)}–{groups.Max(g => g.to)}</size>" : "";
                var button = UiKit.Button(tabs, "Part" + (i + 1), null, null, null, () => ShowPart(index), UiKit.Kind.Ghost, null, 15f, 23);
                int number = part.number;
                LocText.Set(UiKit.LabelOf(button), () => Loc.F("Phần {0}", "Part {0}", "パート{0}", number) + range);
                UiKit.Size(button, 132f, 46f);
                _partTabs.Add(button);
            }
        }

        private void BuildNavigator()
        {
            foreach (Transform child in _navigator) Destroy(child.gameObject);
            _navButtons.Clear();
            var numbers = _test.parts.SelectMany(p => p.groups ?? Array.Empty<IeltsGroup>())
                .Where(g => g.type != "writing_task" && g.type != "speaking_part")
                .SelectMany(g => Enumerable.Range(g.from, g.to - g.from + 1)).Distinct().OrderBy(n => n).ToList();
            foreach (int n in numbers)
            {
                int number = n;
                var cell = new GameObject("Nav" + n, typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(_navigator, false);
                var image = cell.GetComponent<Image>();
                image.sprite = UiKit.Circle;
                image.color = GetAnswer(n).Length > 0 ? AnsweredColor : new Color(1f, 1f, 1f, 0.08f);
                cell.GetComponent<Button>().onClick.AddListener(() => GoToQuestion(number));
                var label = Text((RectTransform)cell.transform, "N", n.ToString(), 12.5f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
                NLUi.Stretch(label.rectTransform);
                label.raycastTarget = false;
                _navButtons[n] = image;
            }
        }
    }
}
