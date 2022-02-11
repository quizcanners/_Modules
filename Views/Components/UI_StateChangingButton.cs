using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.IsItGame.UI
{
    public class UI_StateChangingButton : UnityEngine.MonoBehaviour, IPEGI
    {
        [UnityEngine.SerializeField] private bool _exit;
        [UnityEngine.SerializeField] private Game.Enums.GameState _targetState;
        public void ChangeState()
        {
            if (_exit)
                _targetState.Exit();
            else
                _targetState.Enter();
        }

        public void Inspect()
        {
            pegi.Nl();

            "Exit".PegiLabel().ToggleIcon(ref _exit).Nl();

            "State to {0}".F(_exit ? "Exit" : "Target").PegiLabel(60).EditEnum(ref _targetState).Nl();

            var bttn = GetComponent<UnityEngine.UI.Button>();

            if (bttn && pegi.edit_Listener(bttn.onClick, ChangeState, target:  bttn).Nl())
                bttn.SetToDirty();
        }
    }

    [PEGI_Inspector_Override(typeof(UI_StateChangingButton))] internal class StateChangingButtonDrawer : PEGI_Inspector_Override { }

}
