using QuizCanners.Inspect;
using UnityEngine;
using System.Collections.Generic;
using QuizCanners.Utils;
using System;

namespace QuizCanners.IsItGame {

    [ExecuteAlways]
    public class Singleton_Sounds : IsItGameServiceBase, INeedAttention
    {
        [SerializeField] protected AudioSource originalSource;
        [SerializeField] protected SO_EnumeratedSounds sounds;

        internal readonly PoolOfSoundSources pool = new();
        private readonly Dictionary<Game.Enums.SoundEffects, Gate.Double> _playTimes = new();
        private readonly List<SoundRequest> _stagingRequests = new();

        private readonly PlayerPrefValue.Bool _wantSounds = new("WantSounds", defaultValue: true);
        private readonly PlayerPrefValue.Float _soundEffectsVolume = new("SoundsVolume", defaultValue: 1f);

        internal const float DEFAULT_GAP = 0.03f;

        public bool WantSound 
        {
            get => _wantSounds.GetValue();
            set 
            {
                _wantSounds.SetValue(value);
                SetDirty();
            }
        }

        private struct SoundRequest
        {
            public Game.Enums.SoundEffects Effect;
            public double DspTime;
            public float VolumeScale;
            public bool AllowFadeOut;
            public float MinGap;

            public bool UsePosition;
            public Vector3 Position;

            public bool IsTimeToPlay => (DspTime - AudioSettings.dspTime) < 0.1f;

            public bool TooLateToPlay => AudioSettings.dspTime > DspTime;
        }

        public bool CanPlay(Game.Enums.SoundEffects eff, float minGap) => CanRegisterNewSound(eff, minGap: minGap);
        
        public void PlayOneShotAt(Game.Enums.SoundEffects eff, Vector3 position, float minGap, float clipVolume = 1, bool allowFadeOut = false)
        {
            if (sounds.TryGet(eff, out AudioClip clip)
                && TryRegisterNewSoundInstance(eff, minGap: minGap)
                && pool.TrySpawn(position, out C_SoundSourceManager inst, transform)) 
                        inst.Play(clip, volume: clipVolume, allowFadeOut: allowFadeOut);
        }

        private bool TryPlayOneShotScheduled(SoundRequest req)
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

        public void Play(Game.Enums.SoundEffects eff, float minGap, float clipVolume) => Play(eff, originalSource, minGap: minGap, clipVolume: clipVolume);

        public void Play(Game.Enums.SoundEffects effect, AudioSource targetSource, float minGap, float clipVolume)
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
                    PlayOneShot_Internal(clip, targetSource, clipVolume);
                }
            }
        }

        public void PlayDelaed(Game.Enums.SoundEffects eff, float delay, float clipVolume = 1)
        {
            var req = new SoundRequest()
            {
                Effect = eff,
                DspTime = AudioSettings.dspTime + delay,
                VolumeScale = clipVolume,
            };

            _stagingRequests.Add(req);
        }

        public void PlayDelaedAt(Game.Enums.SoundEffects eff, Vector3 position, float delay, float minGap = DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
        {
            var req = new SoundRequest()
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


        private void PlayOneShot_Internal(AudioClip clip, AudioSource targetSource, float clipVolume = 1) =>
          targetSource.PlayOneShot(clip, volumeScale: clipVolume * _soundEffectsVolume.GetValue());

        private bool TryRegisterNewSoundInstance(Game.Enums.SoundEffects eff, float minGap)
        {
            var lastPlayed = _playTimes.GetOrCreate(eff);
            return lastPlayed.TryChange(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        private bool CanRegisterNewSound(Game.Enums.SoundEffects eff, float minGap)
        {
            var lastPlayed = _playTimes.GetOrCreate(eff);
            return lastPlayed.IsDirty(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        public void Reset()
        {
            if (!originalSource)
                originalSource = gameObject.GetComponentInChildren<AudioSource>();
        }

        public void LateUpdate()
        {
            if (_stagingRequests.Count > 0) 
            {
                for (int i=_stagingRequests.Count-1; i>=0; i--) 
                {
                    SoundRequest req = _stagingRequests[i];

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

        public override void InspectInList(ref int edited, int ind)
        {
            var s = WantSound;
            pegi.ToggleIcon(ref s).OnChanged(()=> WantSound = s);
            base.InspectInList(ref edited, ind);

        }

        public override string InspectedCategory => Singleton.Categories.SCENE_MGMT;

        private readonly pegi.EnterExitContext context = new();

        private Game.Enums.SoundEffects _debugSound;
        public override void Inspect()
        {
            base.Inspect();

            using (context.StartContext())
            {
                if (!originalSource)
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

                var mgmt = Singleton.Get<Singleton_Sounds>();

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

    public static class SoundEffectsExtension 
    {
        public static void PlayOneShotAt(this Game.Enums.SoundEffects eff, Vector3 position, float minGap = Singleton_Sounds.DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
           => Singleton.Try<Singleton_Sounds>(serv => serv.PlayOneShotAt(eff, position, minGap: minGap, clipVolume: clipVolume, allowFadeOut: allowFadeOut));

        public static void PlayOneShotDelayedAt(this Game.Enums.SoundEffects eff, Vector3 position, float delay, float minGap = Singleton_Sounds.DEFAULT_GAP, float clipVolume = 1, bool allowFadeOut = false)
           => Singleton.Try<Singleton_Sounds>(serv => 
           serv.PlayDelaedAt(eff, position, minGap: minGap, delay: delay, clipVolume: clipVolume, allowFadeOut: allowFadeOut));

        public static bool CanPlay(this Game.Enums.SoundEffects eff, float minGap = Singleton_Sounds.DEFAULT_GAP) 
              => Singleton.TryGetValue<Singleton_Sounds, bool>(serv => serv.CanPlay(eff, minGap: minGap), defaultValue: false);

        public static void PlayOneShot(this Game.Enums.SoundEffects eff, float minGap = Singleton_Sounds.DEFAULT_GAP, float clipVolume = 1)
            => Singleton.Try<Singleton_Sounds>(serv => serv.Play(eff, minGap: minGap, clipVolume: clipVolume));

        public static void PlayOneShot(this Game.Enums.SoundEffects eff, AudioSource source, float minGap = Singleton_Sounds.DEFAULT_GAP, float clipVolume = 1)
           => Singleton.Try<Singleton_Sounds>(serv => serv.Play(eff, source, minGap: minGap, clipVolume: clipVolume));

    }

    [PEGI_Inspector_Override(typeof(Singleton_Sounds))] internal class AudioControllerDrawer : PEGI_Inspector_Override { }

}
