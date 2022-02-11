using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.IsItGame.StateMachine
{
    partial class GameState
    {
        public abstract class Base : IsItGameClassBase, IPEGI, IPEGI_ListInspect
        {
            public bool IsCurrent => Game.State.IsCurrent(this);
            protected void SetNextState<T>() where T : Base, new()
            {
                var myType = GetType();
                Game.State.ReturnToState(myType);
                if (typeof(T) != myType)
                {
                    Game.State.Enter<T>();
                }
            }

            internal virtual void OnIsCurrentChange() { }
            internal virtual void OnEnter() { }
            internal virtual void OnExit() { }
            internal virtual void Update() { }
            internal virtual void LateUpdate() { }
            internal virtual void UpdateIfCurrent() { }
            internal void Exit() => Game.State.Exit(this);

            #region Inspector
            public void Inspect()
            {
                GetType().ToPegiStringType().PegiLabel().Nl();
            }

            public void InspectInList(ref int edited, int ind)
            {
                if (Icon.Enter.Click() | (GetType().ToPegiStringType()).PegiLabel().ClickLabel())
                    edited = ind;

                if (Icon.Copy.Click())
                    pegi.CopyPasteBuffer = GetType().ToPegiStringType();
            }
            #endregion
        }
    }
}