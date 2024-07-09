using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace QuizCanners.Modules.GameStateMachine
{
    public abstract class GameState_Base: IPEGI, IPEGI_ListInspect
    {
        public bool IsCurrent => StateMachine.IsCurrent(this);
        protected void SetNextState<T>() where T : GameState_Base, new()
        {
            var myType = GetType();
            StateMachine.ReturnToState(myType);
            if (typeof(T) != myType)
            {
                StateMachine.Enter<T>();
            }
        }

        public virtual void OnIsCurrentChange() { }
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void UpdateIfCurrent() { }
        public void Exit() => StateMachine.Exit(this);

        #region Inspector

        public override string ToString() => GetType().ToPegiStringType();

        public virtual void Inspect()
        {
            GetType().ToPegiStringType().PegiLabel().Nl();
        }

        public virtual void InspectInList(ref int edited, int ind)
        {
            if (Icon.Enter.Click() | (GetType().ToPegiStringType()).PegiLabel().ClickLabel())
                edited = ind;

            if (Icon.Copy.Click())
                pegi.CopyPasteBuffer = GetType().ToPegiStringType();
        }
        #endregion
    }
    
}