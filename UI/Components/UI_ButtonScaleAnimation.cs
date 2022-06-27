using QuizCanners.Inspect;
using QuizCanners.Lerp;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuizCanners.IsItGame.UI
{
    [DisallowMultipleComponent]
    public class UI_ButtonScaleAnimation : MonoBehaviour, IPEGI, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _animatedContent;
        private readonly float _minSize = 0.85f;
        private readonly float _maxSize = 1.25f;
        private readonly float _speed = 2;
        private readonly float _fadeSpeed = 1f;
        private float _localScale = 1;
        private Direction _direction = Direction.Idle;
        private float _portion;
        private float _holdTime;

        private float MaxSize => (1 + (_maxSize - 1) * _portion);
        private float MinSize => (1 - (1 - _minSize) * _portion);

        private enum Direction { Idle, Upscale, Downscale, WobbleUp, FadeToNormal,
            PressedShakeDown, PressedShakeUp
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.eligibleForClick)
            {
                _direction = Direction.PressedShakeDown;
            }
        }
        public virtual void OnPointerUp(PointerEventData eventData) => Wobble();
        public virtual void OnPointerExit(PointerEventData eventData) 
        {
            _direction = Direction.FadeToNormal;
        }

        public void Wobble()
        {
            _direction = Direction.Upscale;
            enabled = true;
            _portion = Mathf.Min(1, _portion + 0.33f * (1 - _portion));
        }

        public void WobbleOnHold()
        {
            enabled = true;
            _holdTime = Time.time;
        }

        void OnDisable()
        {
            _localScale = 1;
            _portion = 0;
            _direction = Direction.Idle;
            transform.localScale = new Vector3(_localScale, _localScale, _localScale);
        }

        void Update()
        {
            switch (_direction)
            {
                case Direction.Idle:

                    bool holding = (Time.time - _holdTime) < 0.2f;

                    if (holding || _portion > 0f)
                    {
                        LerpUtils.IsLerpingBySpeed(ref _portion, holding ? 1 : 0, 5, unscaledTime: true);
                        _localScale = 1 + _portion * (Mathf.Sin(Time.time * 50) + 1) * 0.25f * (MinSize - 1);
                    }
                    else
                    {
                        enabled = false;
                    }

                    break;

                case Direction.PressedShakeDown:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 0.92f, _speed, unscaledTime: true))
                        _direction = Direction.PressedShakeUp;

                    Game.Enums.UiSoundEffects.Ice.PlayOneShot(clipVolume: 0.2f);

                    break;

                case Direction.PressedShakeUp:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 0.97f, _speed, unscaledTime: true))
                        _direction = Direction.PressedShakeDown;

                    Game.Enums.UiSoundEffects.Ice.PlayOneShot(clipVolume: 0.2f);

                    break;

                case Direction.Upscale:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, MaxSize, _speed, unscaledTime: true))
                    {
                        _portion *= 0.66f;
                        _direction = Direction.Downscale;
                    }

                    break;

                case Direction.Downscale:

                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, MinSize, _fadeSpeed, unscaledTime: true))
                    {
                        _portion *= 0.66f;
                        _direction = Direction.WobbleUp;
                    }
                    break;

                case Direction.WobbleUp:


                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 1.01f, _speed, unscaledTime: true))
                    {
                        _portion *= 0.66f;
                        _direction = Direction.FadeToNormal;
                    }


                    break;

                case Direction.FadeToNormal:

                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 1, _fadeSpeed, unscaledTime: true))
                    {
                        _portion = 0;
                        _direction = Direction.Idle;
                    }
                    break;
            }

            _animatedContent.localScale = new Vector3(_localScale, _localScale, _localScale);
        }

        public void Inspect()
        {
            "Animated Content".PegiLabel(90).Edit_IfNull(ref _animatedContent, gameObject).Nl();
            "Wobble".PegiLabel().Click().Nl().OnChanged(Wobble);
        }
    }
    
    [PEGI_Inspector_Override(typeof(UI_ButtonScaleAnimation))] internal class ButtonScaleAnimationDrawer : PEGI_Inspector_Override { }

}