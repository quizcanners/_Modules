using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame.UI
{
    [DisallowMultipleComponent]
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

            (_exit ? "Exit From" : "Enter =>").PegiLabel(60).Edit_Enum(ref _targetState);

            if (Application.isPlaying && Icon.Play.Click())
                ChangeState();

            pegi.Nl();

            var bttn = GetComponent<UnityEngine.UI.Button>();

            if (bttn && pegi.edit_Listener(bttn.onClick, ChangeState, target:  bttn).Nl())
                bttn.SetToDirty();
        }
    }

    [PEGI_Inspector_Override(typeof(UI_StateChangingButton))] internal class StateChangingButtonDrawer : PEGI_Inspector_Override { }

}
