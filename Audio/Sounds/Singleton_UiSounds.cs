using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class Singleton_UiSounds : Base_SoundsSingleton<Game.Enums.SoundEffects>
    {
        [SerializeField] protected AudioSource uiSource;
        [SerializeField] protected SO_EnumeratedSounds sounds;

        private readonly PlayerPrefValue.Bool _wantSounds = new("WantUiSounds", defaultValue: true);
        private readonly PlayerPrefValue.Float _soundEffectsVolume = new("UiSoundsVolume", defaultValue: 1f);

        public override bool WantSound
        {
            get => gameObject.activeSelf && _wantSounds.GetValue();
            set
            {
                _wantSounds.SetValue(value);
                SetDirty();
            }
        }


        public void Play(Game.Enums.SoundEffects effect, float minGap, float clipVolume)
        {
            if (TryRegisterNewSoundInstance(effect, minGap: minGap))
            {
                if (!sounds)
                {
                    QcLog.ChillLogger.LogErrorOnce(() => QcLog.IsNull(sounds, context: "Play"), key: "No Clips", this);
                    return;
                }

                if (sounds.TryGet(effect, out var clip))
                {
                    uiSource.PlayOneShot(clip, volumeScale: clipVolume * _soundEffectsVolume.GetValue());
                }
            }
        }

        #region Inspector

        private readonly pegi.EnterExitContext context = new();

        private Game.Enums.SoundEffects _debugSound;
        public override void Inspect()
        {
            base.Inspect();

            using (context.StartContext())
            {
                if (!uiSource)
                {
                    "Find Audio Source".PegiLabel().Click().Nl().OnChanged(Reset);
                }

                if (context.IsAnyEntered == false)
                {
                    _soundEffectsVolume.Nested_Inspect().Nl();

                    if (Application.isPlaying)
                    {
                        if ("Sound".PegiLabel(60).Edit_Enum(ref _debugSound) | Icon.Play.Click().Nl())
                            _debugSound.PlayOneShot();
                    }
                    else
                    {
                        pegi.Nl();
                        "Can test in Play Mode only".PegiLabel().WriteHint();
                    }
                }

                "Sounds".PegiLabel().Edit_Enter_Inspect(ref sounds).Nl();

                if ("Settings".PegiLabel().IsConditionally_Entered(canEnter: uiSource))
                {
                    var changed = pegi.ChangeTrackStart();

                    if (Application.isPlaying)
                        "Changes will not be saved after exiting play mode".PegiLabel().WriteWarning();

                    pegi.TryDefaultInspect(uiSource);
                }

                pegi.Nl();
            }
        }

        public override string NeedAttention()
        {
            if (!sounds)
                return "No Sounds Scriptable Object";

            if (!uiSource)
                return "No Audio Source";

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

    [PEGI_Inspector_Override(typeof(Singleton_UiSounds))] internal class Singleton_UiSoundsDrawer : PEGI_Inspector_Override { }

    public static partial class SoundEffectsExtension
    {
        public static void PlayOneShot(this Game.Enums.SoundEffects eff, float minGap = Singleton_WorldSounds.DEFAULT_GAP, float clipVolume = 1)
            => Singleton.Try<Singleton_UiSounds>(serv => serv.Play(eff, minGap: minGap, clipVolume: clipVolume));
    }
}