using QuizCanners.Inspect;
using QuizCanners.IsItGame.StateMachine;
using QuizCanners.IsItGame.UI;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    [ExecuteAlways]
    public class Singleton_GameController : Singleton.BehaniourBase, IPEGI
    {
        public const string PROJECT_NAME = "Is It A Game";

        public SO_PersistentGameData PersistentProgressData;

        void Update()
        {
            GameState.Machine.ManagedUpdate();

            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();
        }

        void LateUpdate() => GameState.Machine.ManagedLateUpdate();

        protected override void AfterEnable()
        {
            GameState.Machine.ManagedOnEnable();

            if (PersistentProgressData)
                PersistentProgressData.Load();
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            GameState.Machine.ManagedOnDisable();
            if (PersistentProgressData)
                PersistentProgressData.Save();
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
                    "GAME CONTROLLER".PegiLabel(pegi.Styles.ListLabel).Write();

                pegi.Nl();

                "Modules".PegiLabel().IsEntered().Nl().If_Entered(Singleton.Collector.Inspect);  // Independent

                if ("State Machine".PegiLabel().IsEntered().Nl())
                    GameState.Machine.Inspect(); // Game Flow logic.

                "Persistent Data".PegiLabel().Edit_Enter_Inspect(ref PersistentProgressData).Nl();  // Game Data that changes from run to run

                "Utils".PegiLabel().IsEntered().Nl().If_Entered(QcUtils.InspectAllUtils);

                if (context.IsAnyEntered == false && Application.isPlaying)
                    Singleton.Try<Singleton_UiView>(s => s.InspectCurrentView(),
                        onFailed: () => "No {0} found".F(nameof(Singleton_UiView)));
            }
        }
        #endregion
    }

    [PEGI_Inspector_Override(typeof(Singleton_GameController))] internal class GameManagerDrawer : PEGI_Inspector_Override { }
}