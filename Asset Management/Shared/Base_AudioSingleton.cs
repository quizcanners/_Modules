using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace QuizCanners.Modules.Audio
{
    using Inspect;
    using Lerp;
    using Utils;

    public abstract class Base_AudioSingleton : Singleton.BehaniourBase
    {
        public AudioSource sourceA;
        public AudioSource sourceB;

        [SerializeField] protected AudioMixerGroup _musicMixerGroup;

        [NonSerialized] protected AudioClip _newTargetClip;
        [NonSerialized] protected float _startTargetClipAt;
        [NonSerialized] protected bool _loadingNextClip;
        [NonSerialized] protected float _transitionProgress;
        [NonSerialized] protected float _currentVolume = 1;
        [NonSerialized] protected bool _activeIsA;
        [NonSerialized] protected int _latestRequestVersion;
        [NonSerialized] protected bool _applicationPaused;

        [NonSerialized] protected float _targetSongVolumeScale = 1;
        [NonSerialized] protected float _activeSongVolumeScale = 1;
        [NonSerialized] protected float _fadingSongVolumeScale = 1;

        protected readonly Dictionary<string, float> _perSongPlaybeckResume = new();

        public abstract float UserPreferencesVolume
        { get; set; }

        private bool _noSong;

        public virtual bool IsPlayingNothing { get; }

        protected AudioSource ActiveSource => _activeIsA ? sourceA : sourceB;
        protected AudioSource FadingSource => _activeIsA ? sourceB : sourceA;

        public float Progress
        {
            get => ActiveSource.clip ? (ActiveSource.time / ActiveSource.clip.length) : 0;
            set
            {
                if (!ActiveSource.clip)
                    return;
                ActiveSource.time = value * ActiveSource.clip.length;
            }
        }

        protected void Play_Internal(SO_Music_ClipData data, bool skipTransition = false)
        {
            if (data == null)
            {
                Play_Internal_Clip(null, songVolumeScale: 0, skipTransition, startAt: 0);
                return;
            }

            _latestRequestVersion += 1;
            int thisRequest = _latestRequestVersion;
            _loadingNextClip = true;

            StartCoroutine(data.GetClipAsync(onComplete: clip =>
            {
                if (_latestRequestVersion > thisRequest)
                {
                    return;
                }

                float startAt;
                if (data.AlwaysStartFromBeginning || !clip || !_perSongPlaybeckResume.TryGetValue(clip.name, out startAt))
                    startAt = 0;

                Play_Internal_Clip(clip, songVolumeScale: data.Volume, skipTransition: skipTransition, startAt: startAt);
            }));
        }

        private void Play_Internal_Clip(AudioClip clip, float songVolumeScale,  bool skipTransition = false, float startAt = 0)
        {
            _noSong = !clip;
            _latestRequestVersion += 1;
            _newTargetClip = clip;
            _startTargetClipAt = startAt;
            _loadingNextClip = false;
            _targetSongVolumeScale = songVolumeScale;

            if (skipTransition)
            {
                Flip();
                _transitionProgress = 1;
            }
        }

        private void Flip()
        {
            // Save Where we stopped playing
            var curClip = ActiveSource.clip;

            if (curClip)
            {
                if (ActiveSource.time < curClip.length * 0.5)
                    _perSongPlaybeckResume[curClip.name] = ActiveSource.time;
                else
                    _perSongPlaybeckResume.Remove(curClip.name);
            }

            // Flip Sources
            _activeIsA = !_activeIsA;
            _transitionProgress = 1 - _transitionProgress;
            _fadingSongVolumeScale = _activeSongVolumeScale;

            // Set Up new Active Source


            _activeSongVolumeScale = _targetSongVolumeScale;
            ActiveSource.PlayScheduled(AudioSettings.dspTime + 3);
            ActiveSource.clip = _newTargetClip;

            if (_newTargetClip)
            {
                ActiveSource.time = _startTargetClipAt;
                ActiveSource.loop = true;
                if (_newTargetClip.loadState != AudioDataLoadState.Loaded)
                {
                    ActiveSource.Pause();
                    ActiveSource.PlayScheduled(AudioSettings.dspTime + 3);
                }
                else
                {
                    ActiveSource.PlayScheduled(AudioSettings.dspTime + 2);
                }
            }

            // Clear to avoid Repeat checks
            _newTargetClip = null;
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            base.OnBeforeOnDisableOrEnterPlayMode(afterEnableCalled);

            if (sourceA) 
            {
                sourceA.Stop();
                sourceA.clip = null;
            }

            if (sourceB)
            {
                sourceB.Stop();
                sourceB.clip = null;
            }
        }

        protected override void OnAfterEnable()
        {
            base.OnAfterEnable();

            if (_musicMixerGroup)
            {
                if (sourceA)
                    sourceA.outputAudioMixerGroup = _musicMixerGroup;

                if (sourceB)
                    sourceB.outputAudioMixerGroup = _musicMixerGroup;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            _applicationPaused = pause;
        }

        protected abstract void UpdateClip();
        protected virtual void OnUpdate() { }

        protected virtual float TargetVolume => (_applicationPaused || IsPlayingNothing || _noSong) ? 0 : UserPreferencesVolume;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateClip();

            OnUpdate();

            float volumeChangeSpeed = _applicationPaused ? 2 : 0.1f;

            bool changingVolume = QcLerp.IsLerpingBySpeed(ref _currentVolume, TargetVolume, volumeChangeSpeed, unscaledTime: true);

            if (QcLerp.IsLerpingBySpeed(ref _transitionProgress, 1, 0.2f, unscaledTime: true) || changingVolume)
            {
                ActiveSource.volume = _transitionProgress * _currentVolume * _activeSongVolumeScale;
                FadingSource.volume = (1 - _transitionProgress) * _currentVolume * _fadingSongVolumeScale;

                if (_currentVolume == 0)
                {
                    if (FadingSource.clip)
                    {
                        FadingSource.clip = null;
                        FadingSource.Pause();
                    }

                    if (_noSong)
                    {
                        ActiveSource.clip = null;
                        ActiveSource.Pause();
                    }
                }
                else if (Mathf.Approximately(_transitionProgress, 1))
                {
                 //   if (FadingSource.clip)
                   //     Debug.Log("Clearing " + FadingSource.clip);

                    FadingSource.clip = null;
                    FadingSource.Pause();
                }
            }

            if (_newTargetClip)
            {
                if (_newTargetClip == ActiveSource.clip)
                {
                    _newTargetClip = null;
                    return;
                }

                if (_newTargetClip == FadingSource.clip)
                {
                    Flip();
                    return;
                }
              
                if (_transitionProgress >= 1)
                {
                    Flip();
                }
                
                return;
            }
            
            if (!_loadingNextClip && ActiveSource.clip && !IsPlayingNothing)
            {
                if (ActiveSource.clip.length - ActiveSource.time < 5)
                {
                    TryPlayNextVariant();
                }
            }
            
        }

        protected abstract bool TryPlayNextVariant();

        #region Inspector
        public override string InspectedCategory => Singleton.Categories.AUDIO;


        private readonly pegi.EnterExitContext _context = new(playerPrefId: "MskBase");


        protected virtual void InspectContext(pegi.EnterExitContext context) { }

        public override void Inspect()
        {
            base.Inspect();

            using (_context.StartContext())
            {
                if (_context.IsAnyEntered == false)
                {
                    if (ActiveSource && ActiveSource.clip)
                    {
                        var p = Progress;
                        if ("Progress".PegiLabel(80).Edit_01(ref p).Nl())
                            Progress = p;
                    }

                    var v = UserPreferencesVolume;
                    "Volume (User)".PegiLabel(50).Edit(ref v, 0, 2).Nl(() => UserPreferencesVolume = v);

                    "Volume Scale (Song) {0}%".F(Mathf.FloorToInt(_currentVolume * 100)).PegiLabel().Nl();

                    if (!sourceA && !sourceB)
                    {
                        "No Audio Sources Assigned".PegiLabel().WriteWarning();

                        "Create Audio Sources".PegiLabel().Click().Nl().OnChanged(() =>
                        {
                            var obj = new GameObject("Source A", typeof(AudioSource));
                            obj.transform.SetParent(transform);
                            sourceA = obj.GetComponent<AudioSource>();
                            sourceA.playOnAwake = false;

                            obj = new GameObject("Source B", typeof(AudioSource));
                            obj.transform.SetParent(transform);
                            sourceB = obj.GetComponent<AudioSource>();
                            sourceB.playOnAwake = false;
                        });
                    }
                    else if (Application.isPlaying && ActiveSource && FadingSource)
                        {
                            "Active Playing: {0} Clip: {1}".F(ActiveSource.isPlaying, ActiveSource.clip).PegiLabel().Nl();
                            "Fading Playing: {0} Clip: {1}".F(FadingSource.isPlaying, FadingSource.clip).PegiLabel().Nl();
                        }
                    
                }

                InspectContext(_context);
            }
        }

        public override string NeedAttention()
        {
            if (!_musicMixerGroup)
                return "Mixer Group Not Assigned";

            if (!IsPlayingNothing && TargetVolume == 0)
                return "Target volume is 0";

            return base.NeedAttention();
        }


        #endregion
    }
}
