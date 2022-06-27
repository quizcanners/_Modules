using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.SpecialEffects
{
    public class C_CurrencyAnimationElement : MonoBehaviour, IPEGI
    {
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _image;

        [NonSerialized] public double ValueToDeliver;
        [NonSerialized] private Vector2 _innitialAxxeleration;
        [NonSerialized] internal Pool_CurrencyAnimationController.CurrencyHub Currency;
        [NonSerialized] internal SO_CurrencyAnimationPrototype Prototype;
        [NonSerialized] private RectTransform _parent;

        private float _speed;
        private float _fadeInalpha;


        const float MAX_SPEED = 4000;
        const float INITIAL_AXXELERATION = 1000;
        const float INITIAL_AXXELERATION_FADE_OUT_SPEED = 250;
        const float FADE_IN_SPEED = 10;
        const float DOWNSIZE_OUT_DISTANCE = 500;
        const float FADE_OUT_DISTANCE = 100;
        const float MIN_SCALE_ON_FADE_OUT = 0.5f;

        private Vector2 _screenPoint;
        private Vector2 _previousPos;

        private Vector2 ScreenPosition
        {
            get => _screenPoint;
            set
            {
                _screenPoint = value;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect: _parent,
                screenPoint: _screenPoint,
                cam: C_UiCameraForEffectsManagement.Camera,
                localPoint: out Vector2 originPosition);

                _rectTransform.anchoredPosition = originPosition;
            }
        }

        internal void Restart(Pool_CurrencyAnimationController.CurrencyHub currency, SO_CurrencyAnimationPrototype prototype, int value, RectTransform parent)
        {
            Prototype = prototype;
            Currency = currency;
            _parent = parent;

            gameObject.SetActive(true);

            _image.sprite = Prototype.GetRandomSprite();
            ScreenPosition = Currency.Request.GetOriginPosition();

            _previousPos = ScreenPosition;

            ValueToDeliver = value;

            _innitialAxxeleration = UnityEngine.Random.insideUnitSphere.XY() * INITIAL_AXXELERATION;
            _speed = 0;
            _fadeInalpha = 0;

        }

        void Update()
        {
            _speed = LerpUtils.LerpBySpeed(_speed, MAX_SPEED, MAX_SPEED, unscaledTime: true);

            if (_innitialAxxeleration.magnitude > 0)
            {
                ScreenPosition += _innitialAxxeleration * Time.unscaledDeltaTime;
                _innitialAxxeleration = LerpUtils.LerpBySpeed(_innitialAxxeleration, Vector2.zero, INITIAL_AXXELERATION_FADE_OUT_SPEED, unscaledTime: true);
            }

            var target = Currency.TargetStack.GetTargetPosition();

            ScreenPosition = LerpUtils.LerpBySpeed(ScreenPosition, target, _speed, unscaledTime: true);



            float dist = Vector2.Distance(ScreenPosition, target); //.magnitude;

            if (dist < 5)
            {
                Singleton.Try<Pool_CurrencyAnimationController>(s =>
                {
                    s.Return(this);
                });
            }
            else
            {
                deltaPos = ScreenPosition - _previousPos;

                var angle = Vector2.Angle(Vector2.up, deltaPos) * (deltaPos.x > 0 ? -1 : 1);

                _rectTransform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

                if (_fadeInalpha < 1)
                {
                    _fadeInalpha = LerpUtils.LerpBySpeed(_fadeInalpha, 1, FADE_IN_SPEED, unscaledTime: true);
                }

                float fadeOutAlpha = Mathf.Clamp01(dist / FADE_OUT_DISTANCE);

                _image.TrySetAlpha(Mathf.Min(_fadeInalpha, fadeOutAlpha));

                _rectTransform.localScale = Vector3.one * (MIN_SCALE_ON_FADE_OUT + Mathf.Clamp01(dist / DOWNSIZE_OUT_DISTANCE) * (1 - MIN_SCALE_ON_FADE_OUT));

                _previousPos = ScreenPosition;
            }
        }

        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
        }


        Vector2 deltaPos;

        public void Inspect()
        {
            if (Application.isPlaying == false) 
            {
                pegi.TryDefaultInspect(this);
                return;
            }

            "Delta: {0}".F(deltaPos).PegiLabel().Nl();

            var angle = Vector2.Angle(Vector2.up, deltaPos);
            "Angle: {0}".F(angle).PegiLabel().Nl();

        }
    }

    [PEGI_Inspector_Override(typeof(C_CurrencyAnimationElement))] internal class C_CurrencyAnimationElementDrawer : PEGI_Inspector_Override { }
}