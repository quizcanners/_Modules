using QuizCanners.Inspect;
using QuizCanners.SpecialEffects;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.IsItGame
{
    public class Singleton_InspectorOnGui : IsItGameServiceBase
    {
        [SerializeField] private List<RectTransform> _InspectorUnderalay;
        [SerializeField] private Button _button; 
        [SerializeField] private InspctorMode _mode;
        [SerializeField] private bool _drawInspectorOnGui;
        private pegi.GameView.Window _window;

        public enum InspctorMode { Mobile, Desktop }

        public bool DrawInspector 
        {
            get => _drawInspectorOnGui;
            set 
            {
                _drawInspectorOnGui = value;
                OnStateChange();
            }
        }

        public void Toggle()
        {
            if (!DrawInspector) 
            {
                Singleton.Try<Singleton_ScreenBlur>(
                    onFound: s => s.RequestUpdate(() => DrawInspector = true, updateBackground: false), 
                    onFailed: ()=> DrawInspector = true
                    );
            } else
            {
                DrawInspector = false;
            }
        }

        protected override void OnAfterEnable()
        {
            gameObject.SetActive(QcDebug.ShowDebugOptions);
            DrawInspector = false;
            OnStateChange();
        }

        private void OnStateChange() 
        {
            _InspectorUnderalay.SetActive_List(_drawInspectorOnGui && _mode == InspctorMode.Mobile);
            _button.gameObject.SetActive(_mode == InspctorMode.Mobile);
        }

        void OnGUI()
        {
            switch (_mode) {
                case InspctorMode.Desktop:
                if (_window == null)
                    _window = new pegi.GameView.Window(windowWidth: 600, windowHeight: 800);
                    
                    break;
                case InspctorMode.Mobile:

                if (!_drawInspectorOnGui)
                    return;

                if (_window == null)
                    _window = new pegi.GameView.Window();

                    break;
            }

            _window.Render(Singleton.Get<Singleton_GameController>());
            
           /*
             else if (QcDebug.ShowDebugOptions)
            {
                _window.Render(this);
            }*/

            switch (pegi.GameView.LatestEvent)
            {
                case pegi.LatestInteractionEvent.Click: Game.Enums.UiSoundEffects.Click.PlayOneShot(); break;
                case pegi.LatestInteractionEvent.SliderScroll: Game.Enums.UiSoundEffects.Scratch.PlayOneShot(); break;
                case pegi.LatestInteractionEvent.Enter: Game.Enums.UiSoundEffects.Tab.PlayOneShot(); break;
                case pegi.LatestInteractionEvent.Exit: Game.Enums.UiSoundEffects.MouseExit.PlayOneShot(); break;
            }
        }

        public override void Inspect()
        {
            if (!pegi.PaintingGameViewUI)
            {
                if ("Show Game View Inspector".PegiLabel().ToggleIcon(ref _drawInspectorOnGui).Nl())
                    DrawInspector = _drawInspectorOnGui;
            }

            "Mode".PegiLabel(60).Edit_Enum(ref _mode).Nl().OnChanged(()=>
            {
                OnStateChange();
                _window = null;
            });

            switch (_mode) 
            {
                case InspctorMode.Mobile:
                    "Button".PegiLabel(60).Edit_IfNull(ref _button, gameObject).Nl();
                    break;
            }

            Utils.Singleton.Collector.Inspect_LoadingProgress();
        }
    }

    [PEGI_Inspector_Override(typeof(Singleton_InspectorOnGui))] internal class InspectorOnGuiServiceDrawer : PEGI_Inspector_Override { }
}
