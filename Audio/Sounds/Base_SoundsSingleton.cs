using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.IsItGame
{
    public abstract class Base_SoundsSingleton<T> : IsItGameServiceBase
    {
        protected readonly Dictionary<T, Gate.Double> _playTimes = new();
        protected readonly Gate.Frame _spawnFrame = new(); // To avoid delaying a sound to the next frame

        internal const float DEFAULT_GAP = 0.03f;

        public abstract bool WantSound { get; set; }

        protected bool CanRegisterNewSound(T eff, float minGap)
        {
            var lastPlayed = _playTimes.GetOrCreate(eff);
            return lastPlayed.IsDirty(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        protected bool TryRegisterNewSoundInstance(T eff, float minGap)
        {
            var lastPlayed = _playTimes.GetOrCreate(eff);
            return lastPlayed.TryChange(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        public bool CanPlay(T eff, float minGap) => CanRegisterNewSound(eff, minGap: minGap);

        protected abstract class SoundRequestBase
        {
            public T Effect;
        }

        #region Inspector

        public override string InspectedCategory => Singleton.Categories.SCENE_MGMT;

        public override void InspectInList(ref int edited, int ind)
        {
            var s = WantSound;
            pegi.ToggleIcon(ref s).OnChanged(() => WantSound = s);
            base.InspectInList(ref edited, ind);
        }

        #endregion
    }
}