using QuizCanners.Inspect;
using UnityEngine;
using QuizCanners.Migration;

namespace QuizCanners.SpecialEffects
{

    [CreateAssetMenu(fileName = FILE_NAME, menuName = "Quiz Canners/" + Singleton_SpecialEffectShaders.ASSEMBLY_NAME + "/Managers/" + FILE_NAME)]
    public class SO_SceneLighting_Cfgs : SO_Configurations_Generic<WeatherConfig>, IPEGI
    {
        public const string FILE_NAME = "Scene Lighting Config";
    }

    public class WeatherConfig : Configuration
    {
        public static Configuration activeConfig;

        protected override Configuration ActiveConfig_Internal
        {
            get { return activeConfig; }
            set
            {
                activeConfig = value;
                Singleton_SceneLighting.inspected.Decode(value);
            }
        }

        public override CfgEncoder EncodeData() => Singleton_SceneLighting.inspected.Encode();

    }
}