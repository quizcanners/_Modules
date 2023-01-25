using QuizCanners.Inspect;
using QuizCanners.IsItGame.StateMachine;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public partial class Game 
    {
        private static Singleton_GameController Mgmt => Singleton.Get<Singleton_GameController>();
        internal static SO_PersistentGameData Persistent => Mgmt.PersistentProgressData;
    }

    public class IsItGameClassBase 
    {
        protected Game Game;
    }
    public abstract class IsItGameOnGuiBehaviourBase : IsItGameBehaviourBase, IPEGI
    {
        public abstract void Inspect();

        private readonly pegi.GameView.Window _window = new(customUpscale: 2);

        protected void OnGUI()
        {
            Singleton.Try<Singleton_InspectorOnGui>(s =>
            {
                if (!s.DrawInspector)
                {
                    _window.Render(this);
                }
            });
        }
    }

    public abstract class IsItGameBehaviourBase : MonoBehaviour
    {
        protected Game Game;
    }

    public abstract class IsItGameServiceBase : Singleton.BehaniourBase
    {
        protected Gate.Integer _checkedStateVersion = new();
        protected bool TryEnterIfStateChanged() => Application.isPlaying && _checkedStateVersion.TryChange(GameState.Machine.Version);

        public override void Inspect()
        {
            base.Inspect();

            pegi.Nl();

            "Checked Version: {0}".F(_checkedStateVersion.CurrentValue).PegiLabel().Write();

            if (Icon.Refresh.Click())
            {
                _checkedStateVersion.TryChange(-1);
            }

            pegi.Nl();

        }

    }
}
