using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


namespace QuizCanners.SpecialEffects
{
    [DisallowMultipleComponent]
    public class UI_BlurTransitionSimple : MonoBehaviour, IPEGI
    {
      
        [SerializeField] private Image _blurImage;
        [SerializeField] private Singleton_ScreenBlur.ProcessCommand mode = Singleton_ScreenBlur.ProcessCommand.Blur;
        [SerializeField] private float _transitionSpeed = 6f;

        private readonly ShaderProperty.MaterialToggle FADE_TO_CENTER = new ShaderProperty.MaterialToggle("FADE_TO_CENTER");
        private readonly ShaderProperty.VectorValue MOUSE_DOWN_POSITION = new ShaderProperty.VectorValue("_qcPp_MousePosition");

        private bool skipFirst;

        [NonSerialized] private MaterialInstancer.ForUiGraphics _materialInstancer;

        private Material Material
        {
            get
            {
                if (_materialInstancer == null)
                    _materialInstancer = new MaterialInstancer.ForUiGraphics(_blurImage);

                return _materialInstancer.MaterialInstance;
            }
        }

        public IDisposable SetObscure(Action onObscured, bool updateBackground = false) => SetObscure(onObscured, mode, updateBackground: updateBackground);

        public IDisposable SetObscure(Action onObscured, Singleton_ScreenBlur.ProcessCommand transitionMode, bool updateBackground = false)
        {
            Singleton.Try<Singleton_ScreenBlur>(x => x.RequestUpdate(onFirstRendered: () =>
            {
                ObscureInternal();
                try
                {
                    onObscured?.Invoke();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }
            }, afterScreenGrab: transitionMode, updateBackground: updateBackground));

            return QcSharp.DisposableAction(()=> Reveal(transitionMode: transitionMode));
        }

        public IEnumerator SetObscureIEnumerator() => SetObscureIEnumerator(mode);

        public IEnumerator SetObscureIEnumerator(Singleton_ScreenBlur.ProcessCommand transitionMode)
        {
            bool done = false;
            SetObscure(onObscured: () => done = true, transitionMode);
            while (!done)
                yield return null;
        }

        public IEnumerator TransitionIEnumerator(bool updateBackground) => TransitionIEnumerator(mode, updateBackground: updateBackground);

        public IEnumerator TransitionIEnumerator(Singleton_ScreenBlur.ProcessCommand transitionMode, bool updateBackground)
        {
            bool done = false;
            Transition(onObscured: () => done = true, transitionMode, updateBackground: updateBackground);
            while (!done)
                yield return null;
        }

        public void Transition(Action onObscured, bool updateBackground) => Transition(onObscured, mode, updateBackground: updateBackground);

        public void Transition(Action onObscured, Singleton_ScreenBlur.ProcessCommand transitionMode, bool updateBackground)
        {
            Singleton.Try<Singleton_ScreenBlur>(s => s.RequestUpdate(onFirstRendered: () =>
            {
                ObscureInternal();
                try
                {
                    onObscured?.Invoke();
                } catch (Exception ex) 
                {
                    Debug.LogException(ex);
                }
                Reveal(transitionMode: transitionMode);
            }, afterScreenGrab: transitionMode, updateBackground: updateBackground));
        }

        protected void ObscureInternal() 
        {
            if (!this)
                return;

            _blurImage.TrySetAlpha(1);
            _blurImage.raycastTarget = true;

            if (Material)
                Material.Set(MOUSE_DOWN_POSITION, (Input.mousePosition.XY() / new Vector2(Screen.width, Screen.height)).ToVector4(1, ((float)Screen.width) / Screen.height));
        }

        public void Reveal(bool skipAnimation = false, Singleton_ScreenBlur.ProcessCommand transitionMode = Singleton_ScreenBlur.ProcessCommand.Nothing)
        {
            _blurImage.raycastTarget = false;

            if (skipAnimation)
            {
                _blurImage.TrySetAlpha(0);
            }
            else
            {
                enabled = true;
                skipFirst = true;

                var mat = Material;
                if (mat)
                {
                    FADE_TO_CENTER.SetOn(mat, transitionMode == Singleton_ScreenBlur.ProcessCommand.ZoomOut);
                }
            }
        }

        protected virtual void Awake()
        {
            Reveal(skipAnimation: true);
        }

        protected void Update()
        {
            if (skipFirst) 
            {
                skipFirst = false;
                return;
            }

            if (_blurImage.color.a > 0)
            {
                _blurImage.IsLerpingAlphaBySpeed(0, _transitionSpeed);
            } else 
            {
                enabled = false;
            }
        }

        public void Inspect()
        {
            pegi.Nl();
            pegi.Edit_IfNull(ref _blurImage, gameObject).Nl();

            "Transition speed".PegiLabel(120).Edit(ref _transitionSpeed).Nl();

            "Transition mode".PegiLabel(140).Edit_Enum(ref mode).Nl();

            if ("Transition + BG".PegiLabel().Click())
                Transition(null, updateBackground: true);

            if ("Transition".PegiLabel().Click())
                Transition(null, updateBackground: false);
        }

        protected virtual void Reset()
        {
            if (!_blurImage)
                _blurImage = GetComponent<Image>();
        }

    }
    
    [PEGI_Inspector_Override(typeof(UI_BlurTransitionSimple))]
    internal class BlurTransitionSimpleDrawer : PEGI_Inspector_Override { }
}
