using NihongoLife.Audio;
using NihongoLife.Core;
using NihongoLife.Learning;
using NihongoLife.UI;
using NUnit.Framework;
using UnityEngine;

namespace NihongoLife.Tests
{
    public class MaxPresetTests
    {
        [TearDown] public void TearDown()
        {
            if (OwnerMaxPreset.Active) OwnerMaxPreset.SetActive(false, graphics: false);
            VoicePrefs.Reset();
        }

        [Test] public void Preset_AppliesMaxAndRestores()
        {
            if (OwnerMaxPreset.Active) OwnerMaxPreset.SetActive(false, graphics: false);
            PlayerPrefs.SetFloat("Volume_BGM", 0.3f);
            AudioPrefs.AmbienceOn = false;
            VoicePrefs.SelfMuted = true;
            VoicePrefs.OpenMic = false;
            VoicePrefs.SpeakerVolume = 60;
            bool reduce = MotionSettings.ReduceMotion;

            OwnerMaxPreset.SetActive(true, graphics: false);
            Assert.IsTrue(OwnerMaxPreset.Active);
            Assert.IsTrue(OwnerMaxPreset.OverlayOn);
            Assert.AreEqual(1f, PlayerPrefs.GetFloat("Volume_BGM"));
            Assert.IsTrue(AudioPrefs.AmbienceOn && AudioPrefs.MusicOn);
            Assert.IsFalse(VoicePrefs.SelfMuted);
            Assert.IsTrue(VoicePrefs.OpenMic);
            Assert.AreEqual(100, VoicePrefs.SpeakerVolume);
            Assert.IsFalse(MotionSettings.ReduceMotion);
            Assert.AreEqual(QualitySettings.names.Length - 1, OwnerMaxPreset.Max().quality);

            OwnerMaxPreset.SetActive(false, graphics: false);
            Assert.IsFalse(OwnerMaxPreset.Active);
            Assert.IsFalse(OwnerMaxPreset.OverlayOn);
            Assert.AreEqual(0.3f, PlayerPrefs.GetFloat("Volume_BGM"), 1e-4);
            Assert.IsFalse(AudioPrefs.AmbienceOn);
            Assert.IsTrue(VoicePrefs.SelfMuted);
            Assert.IsFalse(VoicePrefs.OpenMic);
            Assert.AreEqual(60, VoicePrefs.SpeakerVolume);
            Assert.AreEqual(reduce, MotionSettings.ReduceMotion);
            AudioPrefs.AmbienceOn = true;
        }
    }
}
