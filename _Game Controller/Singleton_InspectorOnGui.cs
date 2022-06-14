using QuizCanners.Inspect;
using QuizCanners.SpecialEffects;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public class Singleton_InspectorOnGui : IsItGameServiceBase
    {
        [SerializeField] private List<RectTransform> _InspectorUnderalay;
        private bool _drawInspectorOnGui;
        private readonly pegi.GameView.Window _window = new pegi.GameView.Window(upscale: 2.5f);

        public bool DrawInspector 
        {
            get => _drawInspectorOnGui;
            set 
            {
                _drawInspectorOnGui = value;
                _InspectorUnderalay.SetActive_List(_drawInspectorOnGui);
            }
        }

        public void Toggle()
        {
            if (!DrawInspector) 
            {
                Utils.Singleton.Try<Singleton_ScreenBlur>(
                    onFound: s => s.RequestUpdate(() => DrawInspector = true, updateBackground: false), 
                    onFailed: ()=> DrawInspector = true
                    );
            } else
            {
                DrawInspector = false;
            }
        }

        protected override void AfterEnable()
        {
            gameObject.SetActive(QcDebug.ShowDebugOptions);
            DrawInspector = false;
        }

        void OnGUI()
        {
            if (_drawInspectorOnGui)
            {
                _window.Render(Singleton.Get<Singleton_GameController>());
            }

           /*
             else if (QcDebug.ShowDebugOptions)
            {
                _window.Render(this);
            }*/
            else return;

            switch (pegi.GameView.LatestEvent)
            {
                case pegi.LatestInteractionEvent.Click: IigEnum_SoundEffects.Click.Play(); break;
                case pegi.LatestInteractionEvent.SliderScroll: IigEnum_SoundEffects.Scratch.Play(); break;
                case pegi.LatestInteractionEvent.Enter: IigEnum_SoundEffects.Tab.Play(); break;
                case pegi.LatestInteractionEvent.Exit: IigEnum_SoundEffects.MouseExit.Play(); break;
            }
        }

        public override void Inspect()
        {
            if (!pegi.PaintingGameViewUI)
            {
                if ("Show Game View Inspector".PegiLabel().ToggleIcon(ref _drawInspectorOnGui).Nl())
                    DrawInspector = _drawInspectorOnGui;
            }

            Utils.Singleton.Collector.Inspect_LoadingProgress();
        }
    }

    [PEGI_Inspector_Override(typeof(Singleton_InspectorOnGui))] internal class InspectorOnGuiServiceDrawer : PEGI_Inspector_Override { }
}
