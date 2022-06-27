using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace QuizCanners.SpecialEffects
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Singleton_SpecialEffectShaders.SO_CREATE_PATH + "Currency Animation/" + FILE_NAME)]
    internal class SO_CurrencyAnimationSettings : ScriptableObject
    {
        public const string FILE_NAME = "Currency Animation Settings";

        public float DELAY_BETWEEN_ANIMATIONS = 0.02f;
        public int MAX_ELEMENTS = 50;
        public float SOUND_EFFECT_MIN_GAP = 0.05f;


    }
}