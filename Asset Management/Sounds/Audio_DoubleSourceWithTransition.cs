using QuizCanners.Lerp;
using System;
using UnityEngine;

namespace QuizCanners.Modules.Audio
{
    [Serializable]
    public class Audio_DoubleSourceWithTransition : Audio_DoubleSource
    {
        private readonly float _fadingSpeed;

        public void FlipPlay(AudioClip clip, float volume = 1, bool randomOffset = false) 
        {
            Flip();



            PlayWithoutFlipping(clip, volume: volume, randomOffset: randomOffset);
        }

        public void ManagedUpdate() 
        {
            if (FadingSource.volume == 0)
                return;

            FadingSource.volume = QcLerp.LerpBySpeed(FadingSource.volume, 0, _fadingSpeed, unscaledTime: true);
        }

        public Audio_DoubleSourceWithTransition(float fadingSpeed) 
        {
            _fadingSpeed = fadingSpeed;
        }
    }
}
