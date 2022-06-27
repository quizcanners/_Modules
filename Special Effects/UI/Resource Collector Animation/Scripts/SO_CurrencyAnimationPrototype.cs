using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    [CreateAssetMenu(fileName = FILE_NAME, menuName = Singleton_SpecialEffectShaders.SO_CREATE_PATH + "Currency Animation/" + FILE_NAME)]
    public class SO_CurrencyAnimationPrototype : ScriptableObject, IPEGI_ListInspect, IPEGI, INeedAttention
    {
        public const string FILE_NAME = "Currency Animation Sprite";

        [SerializeField] private List<Sprite> sprites = new List<Sprite>();
        [SerializeField] private List<AudioClip> onCreateSounds = new List<AudioClip>();
        [SerializeField] private List<AudioClip> onConsumeSounds = new List<AudioClip>();

        [NonSerialized] private int _previousSprite = -1;
        [NonSerialized] private int _previousCreateSound = -1;
        [NonSerialized] private int _previousConsumeSound = -1;

        public Sprite GetRandomSprite() => sprites.GetRandom(ref _previousSprite);
            
        internal bool TryGetRandomCreateSound(out AudioClip clip) 
        {
            clip = onCreateSounds.GetRandom(ref _previousCreateSound);

            return clip;
        }

        internal bool TryGetRandomConsumeSound(out AudioClip clip)
        {
            clip = onConsumeSounds.GetRandom(ref _previousConsumeSound);

            return clip;
        }

        #region Inspector

        public void Inspect()
        {
            pegi.Nl();

            "Sprites".PegiLabel().Edit_List_UObj(sprites).Nl();
            "On Create".PegiLabel().Edit_List_UObj(onCreateSounds).Nl();
            "On Consume".PegiLabel().Edit_List_UObj(onConsumeSounds).Nl();
        }

        public void InspectInList(ref int edited, int index)
        {
            var first = sprites.TryGet(0);

            if (sprites.Count > 1)
                "x{0}".F(sprites.Count).PegiLabel(40).Write();

            if (pegi.Edit(ref first))
                sprites.ForceSet(0, first);

            if (Icon.Enter.Click())
                edited = index;
        }

        public string NeedAttention()
        {
            foreach (var s in sprites)
                if (!s)
                    return "Missing sprite";

            return null;
        }

        #endregion


        public bool TryRequestAnimation(RectTransform target) =>
            Singleton.Try<Pool_CurrencyAnimationController>(s => s.RequestAnimation(this, target));

        public bool TryRequestAnimation(RectTransform target, double targetValue) =>
          Singleton.Try<Pool_CurrencyAnimationController>(s => s.RequestAnimation(this, target, targetValue));

    }

    

    [PEGI_Inspector_Override(typeof(SO_CurrencyAnimationPrototype))] internal class SO_CurrencyCollectionSettingDrawer : PEGI_Inspector_Override { }
}