using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.SpecialEffects;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuizCanners.IsItGame.UI
{
    [DisallowMultipleComponent]
    public class UI_OnMouseOverEffects : UI_ButtonSoundsAndState, ISelectHandler, IDeselectHandler
#if UNITY_STANDALONE || UNITY_EDITOR
        , IPointerEnterHandler, IPointerExitHandler
#endif
    {
        [SerializeField] private Game.Enums.SoundEffects _mouseEnterSound = Game.Enums.SoundEffects.MouseEnter;
        [SerializeField] private Game.Enums.SoundEffects _mouseExitSound = Game.Enums.SoundEffects.MouseExit;
        [SerializeField] private List<AffectedElement> Elements = new();

        [Header("Optional")]
        [SerializeField] private UI_BlurTransitionSimple _blurTransitionSimple;

        private bool _isHighlighted;

        private static bool _lerpIsHighlighted;

        private bool firstUpdateCompleted;

        public void SetHighlighted(bool value, bool playSound) 
        {
            _isHighlighted = value;
            if (playSound)
                (value ? _mouseEnterSound : _mouseExitSound).PlayOneShot();
        }

#if UNITY_STANDALONE || UNITY_EDITOR
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                _isHighlighted = true;
                _mouseEnterSound.PlayOneShot();
            }
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                _isHighlighted = false;
                _mouseExitSound.PlayOneShot();
            }
        }
#endif
        public void OnSelect(BaseEventData eventData)
        {
            _isHighlighted = true;
            _mouseEnterSound.PlayOneShot();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHighlighted = false;
            _mouseExitSound.PlayOneShot();
        }

        private readonly LerpData ld = new();

        private readonly Gate.Bool isHighlightedGate = new();

        protected override void Update()
        {
            base.Update();

            if (Down) 
                return;

            ld.Reset();

          

            if (_isHighlighted && _blurTransitionSimple && firstUpdateCompleted)
            {
                if (isHighlightedGate.TryChange(_isHighlighted))
                {
                    _blurTransitionSimple.Transition(onObscured: () =>
                    {
                        _lerpIsHighlighted = _isHighlighted;
                        Elements.Portion(ld);
                        Elements.Lerp(ld, canSkipLerp: true);
                    }, updateBackground: false);
                }
            }
            else
            {
                _lerpIsHighlighted = _isHighlighted;
                Elements.Portion(ld);
                Elements.Lerp(ld, canSkipLerp: !firstUpdateCompleted);
                isHighlightedGate.TryChange(_isHighlighted);
            }
            
            firstUpdateCompleted = true;
        }

        [Serializable]
        protected class AffectedElement : ILinkedLerping
        {
            public Graphic Graphic;
            public Color normalColor = Color.white;
            public Color highlightedColor = Color.white;
            private readonly LinkedLerp.ColorValue col = new("Color", 6);

            public void Portion(LerpData ld)
            {
                col.Portion(ld, targetValue: _lerpIsHighlighted ? highlightedColor : normalColor);
            }

            public void Lerp(LerpData ld, bool canSkipLerp)
            {
                col.Lerp(ld, canSkipLerp: canSkipLerp);

                if (Graphic)
                    Graphic.color = col.CurrentValue;
            }

         
        }

    }

    [PEGI_Inspector_Override(typeof(UI_OnMouseOverEffects))] internal class UI_OnMouseOverEffectsDrawer : PEGI_Inspector_Override { }
}