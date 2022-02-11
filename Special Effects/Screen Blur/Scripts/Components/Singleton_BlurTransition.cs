using static QuizCanners.Utils.Singleton;

namespace QuizCanners.SpecialEffects
{
    public class Singleton_BlurTransition : UI_BlurTransitionSimple, IQcSingleton
    {
        public string InspectedCategory => Categories.SCENE_MGMT;

        protected override void Awake() 
        {
            base.Awake();
            Collector.RegisterService(this, typeof(UI_BlurTransitionSimple));
            Collector.RegisterService(this, typeof(Singleton_BlurTransition));
        }
    }
}
