using QuizCanners.Inspect;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuizCanners.IsItGame.UI
{
    [DisallowMultipleComponent]
    public class UI_ButtonSoundsAndState : MonoBehaviour, IPEGI, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] protected Button _button;
        [SerializeField] private Game.Enums.UiSoundEffects _soundToPlay = Game.Enums.UiSoundEffects.Click;

        [Header("Optional")]
        [SerializeField] private UI_ButtonScaleAnimation scaleAnimation;

        private bool _isDown;
        private float _downTime;
        private bool ButtonClickable => _button && _button.interactable && _button.enabled && _button.gameObject.activeInHierarchy;

        public bool ClickContainedHoldEvents { get; private set; }

        public bool TryUseHoldTime(float gap) 
        {
            if (!Down) 
                return false;
            
            if (HoldTime < gap)
                return false;

            ClickContainedHoldEvents = true;
            _soundToPlay.PlayOneShot();
            _downTime += gap;

            return true;
        }

        public float HoldTime => _isDown ? (Time.unscaledTime - _downTime) : 0;
            
        public bool Down 
        {
            get => _isDown;
            private set 
            {
                if (value && !_isDown)
                    _downTime = Time.unscaledTime;

                _isDown = value;
            }
        }

        private void Reset()
        {
            _button = GetComponent<Button>();
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (!ButtonClickable)
            {
                return;
            }

            Down = true;

            Game.Enums.UiSoundEffects.PressDown.PlayOneShot();
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (!ButtonClickable)
            {
                Down = false;
                return;
            }

            if (Down && !eventData.dragging)
            {
                _soundToPlay.PlayOneShot();
            }

            Down = false;

        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if (!ButtonClickable)
            {
                Down = false;
                return;
            }

            if (Down)
            {
                Game.Enums.UiSoundEffects.MouseLeave.PlayOneShot();
                Down = false;
                var vibe = Utils.Singleton.Try<Singleton_DeviceVibration>(x => x.OnNopeVibrate(), logOnServiceMissing: false);
            }
        }

        protected virtual void Update() 
        {
            if (Down && scaleAnimation)
                scaleAnimation.WobbleOnHold();
        }

        #region Inspector
        public void Inspect()
        {
            if (!_button)
            {
                pegi.Edit(ref _button);
                if (Icon.Refresh.Click())
                    _button = GetComponent<Button>();
            }

            pegi.Nl();

            "Sound to play".PegiLabel(80).Edit_Enum(ref _soundToPlay).Nl();

            "Optional".PegiLabel(pegi.Styles.BaldText).Nl();

            "Scale Animation".PegiLabel(90).Edit(ref scaleAnimation);

            if (!scaleAnimation) 
            {
                Icon.Refresh.Click().OnChanged(()=> scaleAnimation = GetComponent<UI_ButtonScaleAnimation>());
                Icon.Add.Click().OnChanged(() => scaleAnimation = gameObject.AddComponent<UI_ButtonScaleAnimation>());
            }

            pegi.Nl();


        }

        #endregion
    }

    [PEGI_Inspector_Override(typeof(UI_ButtonSoundsAndState))] internal class ButtonSoundsAndHoldDrawer : PEGI_Inspector_Override { }

}