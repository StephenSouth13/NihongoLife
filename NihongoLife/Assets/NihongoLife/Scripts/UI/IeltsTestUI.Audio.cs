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
    /// <summary>Partial of IeltsTestUI: part audio, playback, and the audio Update tick. The MonoBehaviour entry point stays IeltsTestUI.cs.</summary>
    public sealed partial class IeltsTestUI : MonoBehaviour
    {
        // ─────────── Audio ───────────

        private IEnumerator PrepareAudio(int partIndex, bool autoplay)
        {
            var part = _test.parts[partIndex];
            string path = IeltsLibrary.AudioPath(_package, part);
            if (path == null) yield break;
            _loadingPart = partIndex;
            _audio.Stop();
            _audioStatus.text = Loc.F("Đang tải audio {0}…", "Loading audio for {0}…", "{0}の音声を読み込み中…", part.title);
            AudioClip clip = null;
            yield return IeltsLibrary.LoadAudio(path, c => clip = c);
            if (_loadingPart != partIndex) yield break; // a newer request replaced this one
            _loadingPart = -1;
            if (clip == null) { _audioStatus.text = Loc.T("Không tải được audio của phần này.", "Could not load this part's audio.", "このパートの音声を読み込めません。"); yield break; }
            if (_audio.clip != null && _audio.clip != clip) Destroy(_audio.clip);
            _audio.clip = clip;
            _audioPart = partIndex;
            _audio.time = Mathf.Clamp(_attempt.audioPositions[partIndex], 0f, Mathf.Max(0f, clip.length - 0.05f));
            if (autoplay) { _audio.Play(); _audioPlaying = true; }
            RefreshAudioBar();
        }

        public void TogglePlay()
        {
            if (_audio.clip == null || IsSubmitted) return;
            if (IsExam)
            {
                // The recording cannot be paused in exam conditions; starting it is allowed.
                if (!_audio.isPlaying) { _audio.Play(); _audioPlaying = true; }
                return;
            }
            if (_audio.isPlaying) { _audio.Pause(); _audioPlaying = false; }
            else { _audio.Play(); _audioPlaying = true; }
            RefreshAudioBar();
        }

        public void Seek(float seconds)
        {
            if (IsExam || _audio.clip == null) return;
            _audio.time = Mathf.Clamp(seconds, 0f, _audio.clip.length - 0.05f);
            RefreshAudioBar();
        }

        private void OnTrackClicked(BaseEventData data)
        {
            if (IsExam || _audio.clip == null || !(data is PointerEventData pointer)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_progressTrack, pointer.position, pointer.pressEventCamera, out Vector2 local);
            float t = Mathf.InverseLerp(_progressTrack.rect.xMin, _progressTrack.rect.xMax, local.x);
            Seek(t * _audio.clip.length);
        }

        private void RefreshAudioBar()
        {
            if (!IsListening) return;
            var clip = _audio.clip;
            float length = clip != null ? clip.length : 0f;
            float time = clip != null ? _audio.time : 0f;
            _progressFill.anchorMax = new Vector2(length > 0f ? time / length : 0f, 1f);
            _audioTime.text = $"{Clock(time)} / {Clock(length)}";
            string partName = _audioPart >= 0 ? _test.parts[_audioPart].title : "—";
            if (IsExam)
            {
                _playLabel.text = _audio.isPlaying ? Loc.T("Đang phát", "Playing", "再生中") : Loc.T("Bắt đầu nghe", "Start listening", "聴き始める");
                _playButton.interactable = !_audio.isPlaying && !IsSubmitted && _attempt.reviewRemaining < 0f;
                _audioStatus.text = _attempt.reviewRemaining >= 0f ? Loc.T("Đã nghe xong các phần", "All parts heard", "全パートを聴き終えました") : Loc.F("Đang nghe: {0}", "Now playing: {0}", "再生中：{0}", partName);
            }
            else
            {
                _playLabel.text = _audio.isPlaying ? Loc.T("Tạm dừng", "Pause", "一時停止") : Loc.T("Phát", "Play", "再生");
                _playButton.interactable = clip != null;
                _audioStatus.text = Loc.F("Audio: {0}", "Audio: {0}", "音声：{0}", partName);
            }
        }

        private void Update()
        {
            if (!IsOpen || _attempt == null) return;
            ApplyResponsiveLayout();
            if (!IsSubmitted) _attempt.elapsedSeconds += Time.unscaledDeltaTime;

            if (IsListening)
            {
                // Exam mode: when a part's recording ends, move straight on to the next one.
                if (IsExam && _audioPlaying && !_audio.isPlaying && _audio.clip != null && _loadingPart < 0)
                {
                    if (_audio.time <= 0.01f || _audio.time >= _audio.clip.length - 0.25f)
                    {
                        _audioPlaying = false;
                        _attempt.partsFinished[_audioPart] = true;
                        _attempt.audioPositions[_audioPart] = _audio.clip.length;
                        int next = FirstUnfinishedPart();
                        if (!_attempt.partsFinished[next]) { StartCoroutine(PrepareAudio(next, true)); if (_viewPart != next) ShowPart(next); }
                        else if (_attempt.reviewRemaining < 0f) _attempt.reviewRemaining = _test.reviewSeconds;
                        _saveAt = Time.unscaledTime;
                    }
                }
                if (!IsExam && _audioPlaying && !_audio.isPlaying && _audio.clip != null && _audio.time <= 0.01f) _audioPlaying = false;
                RefreshAudioBar();
            }

            if (IsExam && !IsSubmitted && _attempt.reviewRemaining >= 0f)
            {
                _attempt.reviewRemaining -= Time.unscaledDeltaTime;
                if (_attempt.reviewRemaining <= 0f) { SubmitNow(); return; }
            }

            // Reading / Writing: one time limit for the whole paper (exam mode submits when it runs out).
            float limit = !IsListening && _test.timeLimitMinutes > 0 ? _test.timeLimitMinutes * 60f : 0f;
            if (limit > 0f && IsExam && !IsSubmitted)
            {
                float left = limit - _attempt.elapsedSeconds;
                if (left <= 0f) { SubmitNow(); return; }
                if (left <= 600f && left + Time.unscaledDeltaTime > 600f) NihongoLife.UI.HudFeed.Post(Loc.T("Còn 10 phút.", "10 minutes left.", "残り10分です。"), NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                if (left <= 60f && left + Time.unscaledDeltaTime > 60f) NihongoLife.UI.HudFeed.Post(Loc.T("Còn 1 phút — bài sẽ tự nộp khi hết giờ.", "1 minute left — the paper submits when time is up.", "残り1分 — 時間切れで自動提出されます。"), NihongoLife.UI.HudFeed.Kind.Warning, 4f);
                _timer.text = $"{Loc.T("Còn lại", "Time left", "残り時間")}: <b>{(left <= 300f ? "<color=#E0644E>" : "")}{Clock(left)}{(left <= 300f ? "</color>" : "")}</b>   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";
            }
            else if (limit > 0f)
                _timer.text = $"{Loc.T("Thời gian", "Time", "経過")}: <b>{Clock(_attempt.elapsedSeconds)}</b> / {_test.timeLimitMinutes} {Loc.T("phút", "min", "分")}   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";
            else
                _timer.text = IsExam && _attempt.reviewRemaining >= 0f
                    ? $"{Loc.T("Kiểm tra lại", "Checking time", "見直し")}: <b>{Clock(Mathf.Max(0f, _attempt.reviewRemaining))}</b>"
                    : $"{Loc.T("Thời gian", "Time", "経過")}: <b>{Clock(_attempt.elapsedSeconds)}</b>   ·   {Loc.T("Đã làm", "Answered", "解答")} {AnsweredCount}/{QuestionCount}";

            if (IsProductive) TickProductive();
            if (_saveAt > 0f && Time.unscaledTime >= _saveAt) Save();
            if (_audioPlaying && Time.frameCount % 120 == 0) Save();
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
