using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class Singleton_UiSounds : SoundsSingleton_Base
    {
        [SerializeField] protected AudioSource uiSource;

        protected override PlayerPrefValue.Float SoundVolumePlayerPref { get; } = new("UiSoundsVolume", defaultValue: 1f);
        protected override PlayerPrefValue.Bool WantSoundPlayerPref { get; } = new("WantUiSounds", defaultValue: true);

        private SoundLimiterGeneric<SO_AudioClipCollection> _limiter = new SoundLimiterGeneric<SO_AudioClipCollection>();

        public void Play(AudioClip clip, float clipVolume)
        {
            if (clip && WantSound)
                uiSource.PlayOneShot(clip, volumeScale: clipVolume * SoundVolumePlayerPref.GetValue());
        }
        public void Play(SO_AudioClipCollection effect, float minGap, float clipVolume)
        {
            if (!effect)
            {
                QcLog.ChillLogger.LogErrorOnce(() => QcLog.IsNull(effect, context: "Play"), key: "No Clips", this);
                return;
            }

            var clip = effect.GetRandom();

            if (!clip)
            {
                QcLog.ChillLogger.LogWarningOnce(() => QcLog.IsNull(clip, context: "Play"), key: "Random clip is empty", effect);
                return;
            }

            if (_limiter.TryRegisterNewSoundInstance(effect, minGap: minGap))
            {
                 Play(clip, clipVolume);
                
            }
        }


        #region Inspector
        protected override void InsideContext()
        {
            if ("Settings".PegiLabel().IsConditionally_Entered(canEnter: uiSource))
            {
                if (Application.isPlaying)
                    "Changes will not be saved after exiting play mode".PegiLabel().WriteWarning();

                pegi.TryDefaultInspect(uiSource);
            }

            pegi.Nl();

            base.InsideContext();
        }

        public override string NeedAttention()
        {
            if (!uiSource)
                return "No Audio Source";

            return base.NeedAttention();
        }
        #endregion
    }
}