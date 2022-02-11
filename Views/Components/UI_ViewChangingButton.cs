using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.IsItGame.Game.Enums;

namespace QuizCanners.IsItGame.UI
{
    public class UI_ViewChangingButton : MonoBehaviour, IPEGI
    {
        [SerializeField] private Role role;
        [SerializeField] private View _targetView;
        [SerializeField] private UiTransitionType _transition;
        [SerializeField] private bool _clearStack;
        [SerializeField] private bool updateBackground;

        private enum Role { OpenView, CloseCurrent }

        public void ChangeView()
        {
            switch (role) 
            {
                case Role.CloseCurrent: Singleton.Try<Singleton_UiView>(s => s.HideCurrent(_transition)); break;
                case Role.OpenView: _targetView.Show(clearStack: _clearStack, _transition, updateBackground: updateBackground); break;
            }
        }

        public void Inspect()
        {
            pegi.Nl();

            "Role".PegiLabel(50).EditEnum(ref role).Nl();
            "Transition".PegiLabel(80).EditEnum(ref _transition).Nl();

            switch (role) 
            {
                case Role.OpenView:
                    "View".PegiLabel(60).EditEnum(ref _targetView).Nl();
                    "Clear Stack".PegiLabel().ToggleIcon(ref _clearStack).Nl();
                    "Update Background".PegiLabel().ToggleIcon(ref updateBackground).Nl();
                    break;
            }

            var bttn = GetComponent<UnityEngine.UI.Button>();

            if (bttn && pegi.edit_Listener(bttn.onClick, ChangeView, target: bttn).Nl())
                bttn.SetToDirty();
        }
    }

    [PEGI_Inspector_Override(typeof(UI_ViewChangingButton))] internal class ViewChangingButtonDrawer : PEGI_Inspector_Override { }
}
