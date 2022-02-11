using QuizCanners.Inspect;
using QuizCanners.Lerp;
using UnityEngine;
using UnityEngine.EventSystems;

namespace QuizCanners.IsItGame.UI
{
    public class UI_ButtonScaleAnimation : MonoBehaviour, IPEGI, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _animatedContent;
        private readonly float _minSize = 0.95f;
        private readonly float _maxSize = 1.15f;
        private readonly float _speed = 1;
        private readonly float _fadeSpeed = 0.5f;
        private float _localScale = 1;
        private Direction _direction = Direction.Idle;
        private float _portion;

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
            _portion = Mathf.Min(1, _portion + 0.33f * (1 - _portion));
        }

        void Update()
        {
            switch (_direction)
            {
                case Direction.Idle: break;

                case Direction.PressedShakeDown:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 0.92f, _speed))
                        _direction = Direction.PressedShakeUp;

                    IigEnum_SoundEffects.Ice.Play(clipVolume: 0.2f);

                    break;

                case Direction.PressedShakeUp:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 0.97f, _speed))
                        _direction = Direction.PressedShakeDown;

                    IigEnum_SoundEffects.Ice.Play(clipVolume: 0.2f);

                    break;

                case Direction.Upscale:
                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, MaxSize, _speed)) 
                    {
                        _portion *= 0.66f;
                        _direction = Direction.Downscale;
                    }

                    break;

                case Direction.Downscale:

                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, MinSize, _fadeSpeed))
                    {
                        _portion *= 0.66f;
                        _direction = Direction.WobbleUp;
                    }
                    break;

                case Direction.WobbleUp:


                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 1.01f, _speed))
                    {
                        _portion *= 0.66f;
                        _direction = Direction.FadeToNormal;
                    }


                    break;

                case Direction.FadeToNormal:

                    if (!LerpUtils.IsLerpingBySpeed(ref _localScale, 1, _fadeSpeed))
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