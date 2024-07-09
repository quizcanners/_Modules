using System;
using UnityEngine;

namespace QuizCanners.Modules.Audio
{
    [Serializable]
    public class Audio_DoubleSource
    {
        [SerializeField] private AudioSource _sourceA;
        [SerializeField] private AudioSource _sourceB;

        private bool _activeIsA;

        public AudioSource ActiveSource => _activeIsA ? _sourceA : _sourceB;
        public AudioSource FadingSource => _activeIsA ? _sourceB : _sourceA;

        public void Flip() => _activeIsA = !_activeIsA;

        public void PlayWithoutFlipping(AudioClip clip, float volume = 1, bool randomOffset = false) 
        {
            var audioSource = ActiveSource;

            if (!clip)
            {
                audioSource.clip = null;
                audioSource.Stop();
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
            if (randomOffset)
                audioSource.time = clip.length * UnityEngine.Random.value * 0.5f;
        }

        public bool IsFinished => TimeLeft == 0;

        public float TimeLeft 
        {
            get 
            {
                var src = ActiveSource;
                if (src.clip == null || !src.isPlaying)
                    return 0;

                return Mathf.Max(0, src.clip.length - src.time);
            }
        }

        public float FractionLeft
        {
            get
            {
                var src = ActiveSource;
                if (src.clip == null || !src.isPlaying)
                    return 0;

                return Mathf.Clamp01(src.time/ src.clip.length);
            }
        }

    }
}
