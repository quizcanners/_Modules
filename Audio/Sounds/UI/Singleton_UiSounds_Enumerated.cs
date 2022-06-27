using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class Singleton_UiSounds_Enumerated : Singleton_UiSounds//SoundsSingleton_Generic<Game.Enums.UiSoundEffects>
    {
        [SerializeField] protected SO_EnumeratedSounds sounds;

        private SoundLimiterGeneric<Game.Enums.UiSoundEffects> _limiter = new SoundLimiterGeneric<Game.Enums.UiSoundEffects>();

      
        public void Play(Game.Enums.UiSoundEffects effect, float minGap, float clipVolume)
        {
            if (_limiter.TryRegisterNewSoundInstance(effect, minGap: minGap))
            {
                if (!sounds)
                {
                    QcLog.ChillLogger.LogErrorOnce(() => QcLog.IsNull(sounds, context: "Play"), key: "No Clips", this);
                    return;
                }

                if (sounds.TryGet(effect, out var clip))
                {
                    Play(clip, clipVolume);
                }
            }
        }

        #region Inspector

        private Game.Enums.UiSoundEffects _debugSound;

        public override void Inspect()
        {
            if (!uiSource)
            {
                "Find Audio Source".PegiLabel().Click().Nl().OnChanged(Reset);
            }

            if (!IsAnyEntered) 
            {
                SoundVolumePlayerPref.Nested_Inspect().Nl();

                if (Application.isPlaying)
                {
                    if ("Sound".PegiLabel(60).Edit_Enum(ref _debugSound) | Icon.Play.Click().Nl())
                        _debugSound.PlayOneShot();
                }
                else
                {
                    pegi.Nl();
                    "Can test in Play Mode only".PegiLabel().Write_Hint();
                }
            }

            base.Inspect();
        }

        protected override void InsideContext()
        {
            base.InsideContext();
            "Sounds".PegiLabel().Edit_Enter_Inspect(ref sounds).Nl();
        }
        
        public override string NeedAttention()
        {
            if (!sounds)
                return "No Sounds Scriptable Object";

            return base.NeedAttention();
        }
        #endregion


        private void Reset()
        {
            if (!uiSource)
                uiSource = GetComponent<AudioSource>();

            if (!uiSource)
                uiSource = gameObject.AddComponent<AudioSource>();
        }
    }

    [PEGI_Inspector_Override(typeof(Singleton_UiSounds_Enumerated))] internal class Singleton_UiSoundsDrawer : PEGI_Inspector_Override { }

    public static partial class SoundEffectsExtension
    {
        public static void PlayOneShot(this Game.Enums.UiSoundEffects eff, float minGap = -1, float clipVolume = 1)
            => Singleton.Try<Singleton_UiSounds_Enumerated>(serv => serv.Play(eff, minGap: minGap, clipVolume: clipVolume));
    }
}