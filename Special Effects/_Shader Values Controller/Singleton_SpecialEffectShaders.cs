using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu(QcUtils.QUIZCANNERS + "/Special Effects")]
    public partial class Singleton_SpecialEffectShaders : Singleton.BehaniourBase
    {
        public const string SO_CREATE_PATH = QcUnity.SO_CREATE_MENU + "Special Effects/";

        public Effects.RandomSessionSeedManager RandomSeed = new();
        public Effects.TimeManager EffectsTime = new();
        public Effects.GyroscopeParallaxManager GyroscopeParallax = new();
        public Effects.MousePositionManager MousePosition = new();
        public Effects.NoiseTextureManager NoiseTexture = new();
        public Effects.AmbientOcclusionManager AmbientOcclusion = new();

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

        private void Update()
        {
            AmbientOcclusion.ManagedUpdate();
        }

        private void OnApplicationPause(bool state)
        {
            EffectsTime.OnApplicationPauseManaged(state);
        }

        protected override void OnAfterEnable()
        {
            base.OnAfterEnable();

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

        [SerializeField] private pegi.EnterExitContext enterExitContext = new();

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
                AmbientOcclusion.Enter_Inspect_AsList().Nl();
            }
        }
        #endregion

    }

    [PEGI_Inspector_Override(typeof(Singleton_SpecialEffectShaders))] internal class SpecialEffectShadersServiceDrawer : PEGI_Inspector_Override { }
}