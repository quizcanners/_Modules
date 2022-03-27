using QuizCanners.Inspect;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public partial class Singleton_SpecialEffectShaders : Utils.Singleton.BehaniourBase
    {
        public const string ASSEMBLY_NAME = "Special Effects";

        [SerializeField] public EffectsRandomSessionSeedManager RandomSeed = new EffectsRandomSessionSeedManager();
        [SerializeField] public EffectsTimeManager EffectsTime = new EffectsTimeManager();
        [SerializeField] public GyroscopeParallaxManager GyroscopeParallax = new GyroscopeParallaxManager();
        [SerializeField] public EffectsMousePositionManager MousePosition = new EffectsMousePositionManager();
        [SerializeField] public NoiseTextureManager NoiseTexture = new NoiseTextureManager();

        #region Feeding Events

        public void OnViewChange() 
        {
            EffectsTime.OnViewChange();
        }

        protected void LateUpdate()
        {
            EffectsTime.ManagedLateUpdate();
            GyroscopeParallax.ManagedLateUpdate();
            MousePosition.ManagedLateUpdate();
        }

        private void OnApplicationPause(bool state)
        {
            EffectsTime.OnApplicationPauseManaged(state);
        }

        protected override void AfterEnable()
        {
            RandomSeed.ManagedOnEnable();
            GyroscopeParallax.ManagedOnEnable();
            MousePosition.ManagedOnEnable();
            NoiseTexture.ManagedOnEnable();

            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update += LateUpdate;
                #endif
            }
        }

        protected override void OnBeforeOnDisableOrEnterPlayMode(bool afterEnableCalled)
        {
            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.update -= LateUpdate;
                #endif
            }
        }


        #endregion

        #region Inspector

        public override string InspectedCategory => Utils.Singleton.Categories.RENDERING;

        [SerializeField] private pegi.EnterExitContext enterExitContext = new pegi.EnterExitContext();

        public override void Inspect()
        {
            pegi.Nl();

            using (enterExitContext.StartContext())
            {
                RandomSeed.Enter_Inspect_AsList().Nl();
                EffectsTime.Enter_Inspect_AsList().Nl();
                GyroscopeParallax.Enter_Inspect_AsList().Nl();
                MousePosition.Enter_Inspect_AsList().Nl();
                NoiseTexture.Enter_Inspect_AsList().Nl();
            }
        }
        #endregion

    }

    [PEGI_Inspector_Override(typeof(Singleton_SpecialEffectShaders))] internal class SpecialEffectShadersServiceDrawer : PEGI_Inspector_Override { }
}