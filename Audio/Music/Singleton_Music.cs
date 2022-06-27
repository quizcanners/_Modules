using QuizCanners.Inspect;
using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCanners.Utils;
using QuizCanners.Lerp;
using QuizCanners.IsItGame.StateMachine;

namespace QuizCanners.IsItGame
{
    [ExecuteAlways]
    public class Singleton_Music : IsItGameServiceBase, INeedAttention
    {
        public SO_Music_EnumeratedCollection Music;
        public AudioSource sourceA;
        public AudioSource sourceB;
     
        [NonSerialized] private AudioClip _targetClip;
        [NonSerialized] private float _startTargetClipAt;
        [NonSerialized] private bool _loadingNextClip;
        [NonSerialized] private float _transitionProgress;
        [NonSerialized] private float _currentVolumeScale = 1;
        [NonSerialized] private bool _activeIsA;
        [NonSerialized] private int _latestRequestVersion;
        [NonSerialized] private bool _applicationPaused;
        [NonSerialized] private Game.Enums.Music currentlyPlaying = Game.Enums.Music.None;
        [NonSerialized] private float _targetSongVolumeScale = 1;

        private readonly PlayerPrefValue.Bool _musicIsOn = new("WantMusic", defaultValue: true);
        private readonly PlayerPrefValue.Float _musicVolume = new("MusicVolume", defaultValue: 0.5f);

        private readonly Dictionary<AudioClip, float> _perSongPlaybeckResume = new();

        private AudioSource ActiveSource => _activeIsA ? sourceA : sourceB;
        private AudioSource FadingSource => _activeIsA ? sourceB : sourceA;

        public float Volume
        {
            get => _musicIsOn.GetValue() ? _musicVolume.GetValue() : 0;
            set => _musicVolume.SetValue(value);
        }

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

        public void Play(Game.Enums.Music music, bool skipTransition = false)
        {
            currentlyPlaying = music;

            if (!Music.TryGet(music, out var clip)) 
            {
                Debug.LogWarning("No Music Clip Data for {0}".F(music));
            }


            Play_Internal(clip, skipTransition: skipTransition);
        }

        private void Play_Internal(SO_Music_ClipData data, bool skipTransition = false) 
        {
            _latestRequestVersion += 1;
            int thisRequest = _latestRequestVersion;
            _loadingNextClip = true;

            if (data == null) 
            {
                Debug.LogWarning("Clip Data is null");
                return;
            }

            StartCoroutine(data.GetClipAsync(onComplete: clip => 
            {
                if (_latestRequestVersion > thisRequest)
                {
                    return;
                }

                _targetSongVolumeScale = data.Volume;

                float startAt = (data.AlwaysStartFromBeginning || !clip) ? 0 : _perSongPlaybeckResume.TryGet(clip);

                Play_Internal(clip, skipTransition: skipTransition, startAt: startAt);
            }));
        }

        private void Play_Internal(AudioClip clip, bool skipTransition = false, float startAt = 0)
        {
            _latestRequestVersion += 1;
            _targetClip = clip;
            _startTargetClipAt = startAt;
            _loadingNextClip = false;

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
                    _perSongPlaybeckResume[curClip] = ActiveSource.time;
                else
                    _perSongPlaybeckResume.Remove(curClip);
            }

            // Flip Sources
            _activeIsA = !_activeIsA;
            _transitionProgress = 1 - _transitionProgress;

            // Set Up new Active Source
            ActiveSource.clip = _targetClip;

            if (_targetClip)
            {
                ActiveSource.time = _startTargetClipAt;
                ActiveSource.loop = true;
                if (_targetClip.loadState != AudioDataLoadState.Loaded)
                {
                    ActiveSource.Pause();
                    ActiveSource.PlayScheduled(AudioSettings.dspTime + 3);
                }
                else
                {
                    ActiveSource.Play();
                }
            }
           
            // Clear to avoid Repeat checks
            _targetClip = null;
        }

        private void OnApplicationPause(bool pause)
        {
            _applicationPaused = pause;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (TryEnterIfStateChanged())
            {
                Game.Enums.Music song = currentlyPlaying;
                if (GameState.Machine.TryChangeFallback(ref song, fallbackValue: Game.Enums.Music.None)) 
                    Play(song);
            }

            float lerpSoundTo = _applicationPaused ? 0 : ((_loadingNextClip || currentlyPlaying == Game.Enums.Music.None) ? 0 : Volume * _targetSongVolumeScale);

            float volumeChangeSpeed = _applicationPaused ? 2 : 0.5f;

            bool changingVolume = LerpUtils.IsLerpingBySpeed(ref _currentVolumeScale, lerpSoundTo, volumeChangeSpeed, unscaledTime: true);

            if (LerpUtils.IsLerpingBySpeed(ref _transitionProgress, 1, 1f, unscaledTime: true) || changingVolume)
            {
                ActiveSource.volume = _transitionProgress * _currentVolumeScale;
                FadingSource.volume = (1 - _transitionProgress) * _currentVolumeScale;
                
                if (_currentVolumeScale == 0) 
                {
                    FadingSource.clip = null;
                    FadingSource.Pause();

                    ActiveSource.clip = null;
                    ActiveSource.Pause();

                } else if (Mathf.Approximately(_transitionProgress, 1)) 
                {
                    FadingSource.clip = null;
                    FadingSource.Pause();
                }
            }

            if (_targetClip) 
            {
                if (_targetClip == ActiveSource.clip) 
                {
                    _targetClip = null;

                } else if (_targetClip == FadingSource.clip)
                {
                    Flip();
                }
                else
                {
                    if (_transitionProgress >= 1) 
                    {
                        Flip();
                    }
                }
            } else 
            {
                if (!_loadingNextClip && ActiveSource.clip && currentlyPlaying!= Game.Enums.Music.None) 
                {
                    if (ActiveSource.clip.length - ActiveSource.time < 5) 
                    {
                        if (Music.VariantsCount(currentlyPlaying) > 1 && Music.TryGet(currentlyPlaying, out var nextSong))
                            Play_Internal(nextSong);
                    }
                }
            }
        }

        #region Inspector

        public override void InspectInList(ref int edited, int ind)
        {
            var s = _musicIsOn.GetValue();
            pegi.ToggleIcon(ref s).OnChanged(() => _musicIsOn.SetValue(s));
            base.InspectInList(ref edited, ind);

        }

        public override string InspectedCategory => Singleton.Categories.SCENE_MGMT;
        private Game.Enums.Music _debugCoreMusic = Game.Enums.Music.None;
        private readonly pegi.EnterExitContext _context = new pegi.EnterExitContext(playerPrefId: "MskInsp");

        public override void Inspect()
        {
            base.Inspect();

            using (_context.StartContext())
            {
                if (_context.IsAnyEntered == false)
                {
                    if ("Song To Play".PegiLabel().Edit_Enum(ref _debugCoreMusic) | Icon.Play.Click().Nl())
                        _debugCoreMusic.Play();

                    if (ActiveSource.clip) 
                    {
                        var p = Progress;
                        if ("Progress".PegiLabel(80).Edit_01(ref p).Nl())
                            Progress = p;
                    }

                    var v = Volume;
                    "Volume".PegiLabel(50).Edit(ref v, 0, 2).Nl(() => Volume = v);

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
                    else if (Application.isPlaying)
                    {
                        "Active Playing: {0} Clip: {1}".F(ActiveSource.isPlaying, ActiveSource.clip).PegiLabel().Nl();
                        "Fading Playing: {0} Clip: {1}".F(FadingSource.isPlaying, FadingSource.clip).PegiLabel().Nl();
                    }
                }

                "Music".PegiLabel().Edit_Enter_Inspect(ref Music).Nl();
            }
        }
        public override string NeedAttention()
        {
            if (!Music)
                return "No Music Scriptable Object";

            return base.NeedAttention();
        }
        #endregion
    }

    public static class CoreMusicExtension
    {
        public static void Play(this Game.Enums.Music music, bool skipTransition = false) =>
            Singleton.Try<Singleton_Music>(s => s.Play(music, skipTransition: skipTransition));
    }

    [PEGI_Inspector_Override(typeof(Singleton_Music))] internal class MusicServiceDrawer : PEGI_Inspector_Override { }
}
