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
        internal static GameState.MachineManager State => Mgmt.StateMachine;
    }

    public class IsItGameClassBase 
    {
        protected Game Game;
    }
    public abstract class IsItGameOnGuiBehaviourBase : IsItGameBehaviourBase, IPEGI
    {
        public abstract void Inspect();

        private readonly pegi.GameView.Window _window = new(upscale: 2);

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
        protected Game Game;

        public int Version { get; private set; }
        protected void SetDirty() => Version++;

        protected Gate.Integer _checkedStateVersion = new();
        protected bool TryEnterIfStateChanged() => Application.isPlaying && _checkedStateVersion.TryChange(Game.State.Version);

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
