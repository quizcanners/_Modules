using QuizCanners.Inspect;
using QuizCanners.IsItGame.StateMachine;
using QuizCanners.IsItGame.UI;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.IsItGame.StateMachine.GameState;

namespace QuizCanners.IsItGame
{
    [ExecuteAlways]
    public class Singleton_GameController : Singleton.BehaniourBase, IPEGI
    {
        public const string PROJECT_NAME = "Is It A Game";

        public SO_PersistentGameData PersistentProgressData;

        private Gate.Bool _isFocused = new();

        void Update()
        {
            GameState.Machine.ManagedUpdate();

            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();
        }

        void LateUpdate() => GameState.Machine.ManagedLateUpdate();

        protected override void OnAfterEnable()
        {
            SetActiveStateInternal(true);
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
         
        }

        private void SetActiveStateInternal(bool isActive) 
        {
            if (_isFocused.TryChange(isActive))
            {
                if (!isActive)
                {
                    Machine.ManagedOnDisable();
                    if (PersistentProgressData)
                        PersistentProgressData.Save();
                } else 
                {

                    Machine.ManagedOnEnable();

                    if (PersistentProgressData)
                        PersistentProgressData.Load();
                }
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (Application.isMobilePlatform)
                SetActiveStateInternal(hasFocus);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (Application.isMobilePlatform)
                SetActiveStateInternal(!pauseStatus);
        }

        void Awake() 
        {
            QcDebug.ForceDebugOption(); // To have inspector without building IsDebug
            Time.maximumDeltaTime = 1f / 30f;
            QcLog.LogHandler.SavingLogs = true;
        }

        #region Inspector

        [SerializeField] protected pegi.EnterExitContext context = new();

        public override void Inspect()
        {
            using (context.StartContext())
            {
                if (!context.IsAnyEntered)
                {
                    (_isFocused.CurrentValue ? Icon.Active: Icon.Pause).Draw();
                    "GAME CONTROLLER".PegiLabel(pegi.Styles.ListLabel).Write();
                }
                pegi.Nl();

                "Modules".PegiLabel().IsEntered().Nl().If_Entered(Singleton.Collector.Inspect);  // Independent

                if ("State Machine".PegiLabel().IsEntered().Nl())
                    GameState.Machine.Inspect(); // Game Flow logic.

                "Persistent Data".PegiLabel().Edit_Enter_Inspect(ref PersistentProgressData).Nl();  // Game Data that changes from run to run

                "Utils".PegiLabel().Enter_Inspect(QcUtils.InspectAllUtils).Nl();

                if (context.IsAnyEntered == false && Application.isPlaying)
                    Singleton.Try<Singleton_UiView>(s => s.InspectCurrentView(),
                        onFailed: () => "No {0} found".F(nameof(Singleton_UiView)));
            }
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_GameController))] internal class GameManagerDrawer : PEGI_Inspector_Override { }
}