using static QuizCanners.Utils.Singleton;

namespace QuizCanners.SpecialEffects
{
    public class Singleton_BlurTransition : UI_BlurTransitionSimple, IQcSingleton
    {
        public string InspectedCategory => Categories.SCENE_MGMT;

        public bool IsSingletonActive { get => true; set { } }

        protected void OnEnable() 
        {
            Collector.RegisterSingleton(this, typeof(UI_BlurTransitionSimple));
            Collector.RegisterSingleton(this, typeof(Singleton_BlurTransition));
        }

        protected virtual void OnDestroy()
        {
            Collector.TryRemove(this, typeof(UI_BlurTransitionSimple));
            Collector.TryRemove(this, typeof(Singleton_BlurTransition));
        }
    }
}
