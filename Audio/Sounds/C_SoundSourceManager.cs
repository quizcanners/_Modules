using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using UnityEngine;


namespace QuizCanners.IsItGame
{

    public class C_SoundSourceManager : MonoBehaviour, IPEGI
    {
        private AudioSource _audioSource;
        private bool _allowFadeOut;
        private bool _isFadingOut;

        public int EffectIndex;

        private float ClipProgress => Source.time / Source.clip.length;

        private float Volume
        {
            get => Source.volume;
            set => Source.volume = value;
        }

        private AudioSource Source 
        { 
            get 
            {
                if (!_audioSource) 
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }

                return _audioSource;
            }
        }

        private AudioClip Clip 
        {
            set 
            {
                Source.clip = value;
            }
        }

        public void InstanciateSourceFrom(AudioSource original) 
        {
            if (_audioSource)
                _audioSource.DestroyWhateverComponent();

            _audioSource = Instantiate(original, transform);
            _audioSource.transform.localPosition = Vector3.zero;

        }

        public void PlayScheduled(AudioClip clip, double dspTime , float volume, bool allowFadeOut)
        {
            Volume = volume;
            _allowFadeOut = allowFadeOut;
            _isFadingOut = false;
            Clip = clip;
            Source.PlayScheduled(dspTime);
        }

        public void Play(AudioClip clip, float volume, bool allowFadeOut) 
        {
            Volume = volume;
            _allowFadeOut = allowFadeOut;
            _isFadingOut = false;
            Clip = clip;
            Source.Play();

            if (Application.isEditor && clip)
                gameObject.name = "Sound: {0} Volume: {1} {2}".F(clip.name, volume, allowFadeOut ? "Can Fade" : "");
        }

        void Update() 
        {
            Singleton.Try<Singleton_WorldSounds>(s =>
            {
                if (!Source.isPlaying) 
                {
                    s.ReturnToPool(this);
                    return;
                }

                float soundPolution = Mathf.Clamp01(s.pool.VacancyPortion - ClipProgress);

                if (!_isFadingOut && _allowFadeOut && soundPolution > 0) 
                {
                    _isFadingOut = true;
                }

                if (_isFadingOut) 
                {
                    Volume = LerpUtils.LerpBySpeed(Volume, 0, soundPolution, unscaledTime: true);
                    if (Volume < 0.01f) 
                    {
                        s.pool.ReturnToPool(this);
                    }
                }
            });
        }

        #region Inspector

        public void Inspect()
        {

        }

        #endregion
    }

    [PEGI_Inspector_Override(typeof(C_SoundSourceManager))] internal class C_SoundSourceManagerDrawer : PEGI_Inspector_Override { }

}
