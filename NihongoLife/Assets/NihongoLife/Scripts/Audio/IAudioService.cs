using UnityEngine;
using NihongoLife.Core;

namespace NihongoLife.Audio
{
    public interface IAudioService : IGameService
    {
        void PlayBGM(AudioClip clip, bool loop = true, float volume = 1.0f);
        void StopBGM();
        void PlaySFX(AudioClip clip, float volume = 1.0f);
        void PlayVoice(AudioClip clip, float volume = 1.0f);
        void SetBGMVolume(float volume);
        void SetSFXVolume(float volume);
    }
}
