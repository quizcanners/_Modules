using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class C_CurrencyAnimationConsumer : MonoBehaviour, IPEGI, INeedAttention
    {
        [SerializeField] internal RectTransform rectTransform;
        [SerializeField] private SO_CurrencyAnimationPrototype key;

        [NonSerialized] private bool _initialized;

        private float _upscale = 1;

        const float MAX_UPSCALE = 1.5f;
        const float UPSCALE_PER_ELEMENT = 0.2f;
        const float UPSCALE_FADE_SPEED = 10f;

        public void Wobble()
        {
            _upscale = Mathf.Min(MAX_UPSCALE, _upscale + UPSCALE_PER_ELEMENT);
        }

        void Update() 
        {
            if (_initialized)
            {
                if (_upscale > 1)
                {
                    _upscale = LerpUtils.LerpBySpeed(from: _upscale, to: 1, speed: UPSCALE_FADE_SPEED, unscaledTime: true);
                    rectTransform.localScale = Vector3.one * _upscale;
                }
            }
            else if (key && rectTransform)
            {
                Singleton.Try<Pool_CurrencyAnimationController>(s =>
                {
                    s.RegisterAnimationTarget(key, this);
                    _initialized = true;
                });
            }
        }

        void OnDisable()
        {
            _initialized = false;
            Singleton.Try<Pool_CurrencyAnimationController>(s =>
            {
                s.RemoveAnimationTarget(key, this);
            });
        }

        #region Inspector

        public void Inspect()
        {
            pegi.Nl();
            "Target".PegiLabel().Edit(ref rectTransform).Nl();

            "Currency Key".PegiLabel().Edit(ref key, allowSceneObjects: false).Nl();

            if (rectTransform && rectTransform.pivot != Vector2.one * 0.5f && "Fix Pivot".PegiLabel().Click())
                QcUnity.SetPivotTryKeepPosition(rectTransform, Vector2.one * 0.5f);
        }

        public string NeedAttention()
        {
            if (!key)
                return "No Key";

            if (!rectTransform)
                return "No Rect Transform";

            if (Application.isPlaying && !_initialized)
                return "Not Initialized";

            if (rectTransform.pivot != Vector2.one * 0.5f)
                return "Pivot is not Centered";


            return null;
        }

        #endregion


        void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
        }
    }

    [PEGI_Inspector_Override(typeof(C_CurrencyAnimationConsumer))]
    internal class C_CurrencyAnimationOriginDrawer : PEGI_Inspector_Override { }
}