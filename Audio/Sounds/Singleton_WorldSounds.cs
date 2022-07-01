using QuizCanners.Inspect;
using UnityEngine;
using System.Collections.Generic;
using QuizCanners.Utils;
using System;

namespace QuizCanners.IsItGame
{

    [ExecuteAlways]
    public class Singleton_WorldSounds : Base_SoundsSingleton<Game.Enums.SoundEffects>
    {
        [SerializeField] protected AudioSource originalSource;
        [SerializeField] protected SO_EnumeratedSounds sounds;

        internal readonly PoolOfSoundSources pool = new();
        private readonly List<DelayedSoundRequest> _stagingRequests = new();

        private readonly PlayerPrefValue.Bool _wantSounds = new("WantWorldSounds", defaultValue: true);
        private readonly PlayerPrefValue.Float _soundEffectsVolume = new("WorldSoundsVolume", defaultValue: 1f);

        private readonly Dictionary<Game.Enums.SoundEffects, C_SoundSourceManager> _instances = new Dictionary<Game.Enums.SoundEffects, C_SoundSourceManager>();

        public override bool WantSound 
        {
            get => _wantSounds.GetValue();
            set 
            {
                _wantSounds.SetValue(value);
                SetDirty();
            }
        }

        private class PositionSoundRequests : SoundRequestBase
        {
           
        }

        private class DelayedSoundRequest : SoundRequestBase
        {
            public double DspTime;
            public float VolumeScale;
            public bool AllowFadeOut;
            public float MinGap;

            public bool UsePosition;
            public Vector3 Position;

            public bool IsTimeToPlay => (DspTime - AudioSettings.dspTime) < 0.1f;

            public bool TooLateToPlay => AudioSettings.dspTime > DspTime;
        }

      

        public void PlayOneShotAt(Game.Enums.SoundEffects eff, Vector3 soundPosition, float minGap, float clipVolume = 1, bool allowFadeOut = false)
        {
            if (sounds.TryGet(eff, out AudioClip clip)) 
            {
                if (TryRegisterNewSoundInstance(eff, minGap: minGap)) 
                {
                    if (pool.TrySpawn(soundPosition, out C_SoundSourceManager inst, transform))
                    {
                        inst.Play(clip, volume: clipVolume, allowFadeOut: allowFadeOut);
                        inst.EffectIndex = (int)eff;
                        _instances[eff] = inst;
                    }
                } else 
                {
                    if (_instances.TryGetValue(eff, out C_SoundSourceManager inst)) 
                    {
                        Singleton.Try<Singleton_CameraOperatorGodMode>(cam => 
                        {
                            var cameraPos = cam.transform.position;

                            if (Vector3.Distance(cameraPos, soundPosition) < Vector3.Distance(cameraPos, inst.transform.position))
                            {
                                inst.transform.position = soundPosition;
                               // Debug.Log("Repositioning {0}".F(eff));
                            }
                        });
                    }
                     // Move Existing sound position closer
                }
            }
        }

        internal void ReturnToPool(C_SoundSourceManager effect)
        {
            pool.ReturnToPool(effect);
            _instances.Remove((Game.Enums.SoundEffects)effect.EffectIndex);
        }

        public void PlayDelaedAt(Game.Enums.SoundEffects eff, Vector3 position, float delay, float minGap = DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
        {
            var req = new DelayedSoundRequest()
            {
                Effect = eff,
                DspTime = AudioSettings.dspTime + delay,
                VolumeScale = clipVolume,
                UsePosition = true,
                Position = position,
                MinGap = minGap,
                AllowFadeOut = allowFadeOut,
            };

            _stagingRequests.Add(req);
        }

        private bool TryPlayOneShotScheduled(DelayedSoundRequest req)
        {
            if (sounds.TryGet(req.Effect, out var clip)
                && TryRegisterNewSoundInstance(req.Effect, minGap: req.MinGap)
                && pool.TrySpawn(req.Position, out C_SoundSourceManager inst, transform))
            {
                inst.PlayScheduled(clip, dspTime: req.DspTime, volume: req.VolumeScale, allowFadeOut: req.AllowFadeOut);
                return true;
            }

            return false;
        }


        public void Reset()
        {
            if (!originalSource)
                originalSource = gameObject.GetComponentInChildren<AudioSource>();
        }

        public void LateUpdate()
        {
            _spawnFrame.DoneThisFrame = true;

            if (_stagingRequests.Count > 0) 
            {
                for (int i=_stagingRequests.Count-1; i>=0; i--) 
                {
                    DelayedSoundRequest req = _stagingRequests[i];

                    if (req.TooLateToPlay) 
                    {
                        _stagingRequests.RemoveAt(i);
                        QcLog.ChillLogger.LogErrosExpOnly(() => "Missed the time to play {0}. Discarding", key: req.Effect.ToString(), this);
                    }
                    else if (req.IsTimeToPlay) 
                    {
                        if (req.UsePosition) 
                        {
                            if (TryPlayOneShotScheduled(req))
                                _stagingRequests.RemoveAt(i);
                        } else 
                        {
                            
                        }
                        req.Effect.PlayOneShot(clipVolume: req.VolumeScale);
                    }
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
                if (!originalSource)
                    "Find Audio Source".PegiLabel().Click().Nl().OnChanged(Reset);
                else if (context.IsAnyEntered == false)
                {
                    _soundEffectsVolume.Nested_Inspect().Nl();

                    if (Application.isPlaying)
                        ("Sound".PegiLabel(60).Edit_Enum(ref _debugSound) | Icon.Play.Click().Nl())
                            .OnChanged(() => _debugSound.PlayOneShot());
                    
                }

                "Sounds".PegiLabel().Edit_Enter_Inspect(ref sounds).Nl();

                if ("Settings".PegiLabel().IsConditionally_Entered(canEnter: originalSource)) 
                {
                    var changed = pegi.ChangeTrackStart();

                    if (Application.isPlaying)
                        "Changes will not be saved after exiting play mode".PegiLabel().WriteWarning();

                    pegi.TryDefaultInspect(originalSource);

                    if (changed)
                        pool.ClearAll();
                }

                originalSource.ClickHighlight();
                pegi.Nl();

            }
        }

        public override string NeedAttention()
        {
            if (!sounds)
                return "No Sounds Scriptable Object";

            if (!originalSource)
                return "No Audio Source";
            
            if (originalSource.transform == transform)
                return "Transform should be a child of this transform";

            if (Application.isPlaying && !Singleton.Get<Singleton_CameraOperatorGodMode>())
                return "No {0}".F(nameof(Singleton_CameraOperatorGodMode));

            return base.NeedAttention();
        }
        #endregion

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(afterEnableCalled);
            pool.ClearAll();
        }

        [Serializable]
        internal class PoolOfSoundSources : Pool.PreceduralWithLimits<C_SoundSourceManager>
        {
            public override int MaxInstances => 50;

            protected override C_SoundSourceManager CreateInternal(Transform transform)
            {
                C_SoundSourceManager source = base.CreateInternal(transform);

                var mgmt = Singleton.Get<Singleton_WorldSounds>();

                source.InstanciateSourceFrom(mgmt.originalSource);

                return source;
            }
        }

        private class SoundRequests 
        {
            public Game.Enums.SoundEffects Effect;
            public Vector3 Position;
            public float Volume;

        }
    }

    public static partial class SoundEffectsExtension 
    {
        public static void PlayOneShotAt(this Game.Enums.SoundEffects eff, Vector3 position, float minGap = Singleton_WorldSounds.DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
           => Singleton.Try<Singleton_WorldSounds>(serv => serv.PlayOneShotAt(eff, position, minGap: minGap, clipVolume: clipVolume, allowFadeOut: allowFadeOut));

        public static void PlayOneShotDelayedAt(this Game.Enums.SoundEffects eff, Vector3 position, float delay, float minGap = Singleton_WorldSounds.DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
           => Singleton.Try<Singleton_WorldSounds>(serv => 
           serv.PlayDelaedAt(eff, position, minGap: minGap, delay: delay, clipVolume: clipVolume, allowFadeOut: allowFadeOut));

        public static bool CanPlay(this Game.Enums.SoundEffects eff, float minGap = Singleton_WorldSounds.DEFAULT_GAP) 
              => Singleton.TryGetValue<Singleton_WorldSounds, bool>(serv => serv.CanPlay(eff, minGap: minGap), defaultValue: false);
    }

    [PEGI_Inspector_Override(typeof(Singleton_WorldSounds))] internal class AudioControllerDrawer : PEGI_Inspector_Override { }

}
