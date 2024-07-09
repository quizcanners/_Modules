using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.SpecialEffects
{
    [DisallowMultipleComponent]
    public class C_ImageTransition : MonoBehaviour, IPEGI, INeedAttention
    {
        [SerializeField] private float _transitionSpeed = 4;
        [SerializeField] private Image _image;

        private ShaderProperty.FloatValue TRANSITION = new ShaderProperty.FloatValue("_Transition",0,1);
        private ShaderProperty.TextureValue CURRENT_TEXTURE = new ShaderProperty.TextureValue("_MainTex_Current");
        private ShaderProperty.TextureValue TARGET_TEXTURE = new ShaderProperty.TextureValue("_Next_MainTex");

        private MaterialInstancer.ForUiGraphics materialInstancer;
        private Gate.Bool _textureSet = new Gate.Bool();
        private Texture nextTarget;

        public void SetImmediately(Texture targetTexture) 
        {
            Transition = 1;
            TargetTexture = targetTexture;
            nextTarget = null;
        }

        public void TransitionTo(Texture newTargetTexture) 
        {
            enabled = true;

            if (_textureSet.TryChange(true)) 
            {
                SetImmediately(newTargetTexture);
                return;
            }

            var existingTarget = TargetTexture;
            if (existingTarget && existingTarget == newTargetTexture) 
            {
                return;
            }

            var previous = PreviousTexture;
            if (previous && previous == newTargetTexture) 
            {
                Swap();
                return;
            }

            nextTarget = newTargetTexture;

            if (Transition < 0.4f)
                Swap();
        }

        void Swap() 
        {
            Transition = 1 - Transition;
            Texture currentTarget = TargetTexture;
            TargetTexture = PreviousTexture;
            PreviousTexture = currentTarget;
        }

        private Material GetMaterial() => (materialInstancer ??= new MaterialInstancer.ForUiGraphics(_image)).MaterialInstance;

        private Texture PreviousTexture
        {
            get => CURRENT_TEXTURE.Get(GetMaterial());
            set => CURRENT_TEXTURE.SetOn(GetMaterial(), value);
        }

        private Texture TargetTexture
        {
            get => TARGET_TEXTURE.Get(GetMaterial());
            set => TARGET_TEXTURE.SetOn(GetMaterial(), value);
        }

        private float Transition
        {
            get => GetMaterial().Get(TRANSITION);
            set => GetMaterial().Set(TRANSITION, value);
        }

        private void Update() 
        {
            Transition = QcLerp.LerpBySpeed_Unscaled(Transition, 1, _transitionSpeed * (nextTarget ? 3f : 1f));
            if (Transition == 1) 
            {
                if (nextTarget) 
                {
                    Swap();
                    TargetTexture = nextTarget;
                    nextTarget = null;
                } else 
                {
                    enabled = false;
                }
            }
        }

        void Reset()
        {
            _image.GetComponent<Image>();
        }

        #region Inspector
        void IPEGI.Inspect()
        {
            pegi.Nl();

            "Image".PegiLabel(60).Edit_IfNull(ref _image, gameObject).Nl().OnChanged(()=> 
            {
                materialInstancer = null;
            });

            if (!_image)
                return;

            var transition = Transition;
            if ("Transition".PegiLabel(70).Edit_01(ref transition))
            {
                Transition = transition;
                enabled = false;
            }

            if (Icon.Swap.Click())
            {
                Swap();
            }

            pegi.Nl();

            "Speed".PegiLabel(50).Edit(ref _transitionSpeed, 0.0001f, 25f).Nl();

            if (Application.isPlaying == false || QcUnity.IsPartOfAPrefab(gameObject)) 
            {
                return;
            }

            "Test Transition".PegiLabel(pegi.Styles.ListLabel).Nl();

            TestTransitionTexture(Icon.Cut);
            TestTransitionTexture(Icon.Size);
            TestTransitionTexture(Icon.Green);
            TestTransitionTexture(Icon.Red);
            TestTransitionTexture(Icon.Blue);

            void TestTransitionTexture(Icon icon) 
            {
                if (icon.Click())
                    TransitionTo(icon.GetIcon());
            }


            pegi.Nl();
            "Set Now".PegiLabel(pegi.Styles.ListLabel).Nl();


            SetNowTest(Icon.Active);
            SetNowTest(Icon.InActive);
            SetNowTest(Icon.Green);
            SetNowTest(Icon.Red);
            SetNowTest(Icon.Blue);

            void SetNowTest(Icon icon)
            {
                if (icon.Click())
                    SetImmediately(icon.GetIcon());
            }
        }

        public string NeedAttention()
        {
            if (!_image)
                return "Image not assigned";

            if (_image.type != Image.Type.Simple)
                return "Image Type Simple is expected";

            return null;
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(C_ImageTransition))] internal class C_ImageTransitionDrawer : PEGI_Inspector_Override { }
}