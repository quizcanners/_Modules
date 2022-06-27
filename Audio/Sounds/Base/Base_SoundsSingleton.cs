using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using static UniStorm.UniStorm;

namespace QuizCanners.IsItGame
{
    public abstract class SoundsSingleton_Base : Singleton.BehaniourBase 
    {
        protected readonly Dictionary<SO_AudioClipCollection, Gate.Double> _playTimes_SO = new();

        internal virtual float DEFAULT_GAP => 0.03f;

        protected readonly Gate.Frame _spawnFrame = new(); // To avoid delaying a sound to the next frame
        protected abstract PlayerPrefValue.Bool WantSoundPlayerPref { get; } // = new("WantWorldSounds", defaultValue: true);
        protected abstract PlayerPrefValue.Float SoundVolumePlayerPref { get; }

        public bool WantSound
        {
            get => WantSoundPlayerPref.GetValue();
            set => WantSoundPlayerPref.SetValue(value);
        }

        public float Volume
        {
            get => SoundVolumePlayerPref.GetValue();
            set => SoundVolumePlayerPref.SetValue(value);
        }

        #region Inspector

        public override string InspectedCategory => Singleton.Categories.SCENE_MGMT;

        public override void InspectInList(ref int edited, int ind)
        {
            var s = WantSound;
            pegi.ToggleIcon(ref s).OnChanged(() => WantSound = s);

            if (WantSound)
            {
                var vol = Volume;
                if ("Volme".PegiLabel().Edit(ref vol, 0, 2).Nl())
                    Volume = vol;
            }

            base.InspectInList(ref edited, ind);
        }

        private readonly pegi.EnterExitContext context = new();

        protected bool IsAnyEntered => context.IsAnyEntered;

        protected virtual void InsideContext() 
        {

        }

        public override void Inspect()
        {
            using (context.StartContext())
            {
                InsideContext();
            }
        }

        #endregion

    }

    /*
    public abstract class SoundsSingleton_Generic<T> : SoundsSingleton_Base
    {
        
        protected readonly Dictionary<T, Gate.Double> _playTimes_Enum = new();

        protected bool CanRegisterNewSound(T eff, float minGap)
        {
            if (minGap <= 0)
                minGap = DEFAULT_GAP;

            var lastPlayed = _playTimes_Enum.GetOrCreate(eff);
            return lastPlayed.IsDirty(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        protected bool TryRegisterNewSoundInstance(T eff, float minGap)
        {
            if (minGap <= 0)
                minGap = DEFAULT_GAP;

            var lastPlayed = _playTimes_Enum.GetOrCreate(eff);
            return lastPlayed.TryChange(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        public bool CanPlay(T eff, float minGap) => CanRegisterNewSound(eff, minGap: minGap);
    }
    */

    public class SoundLimiterGeneric<T> 
    {
        internal virtual float DEFAULT_GAP => 0.03f;

        protected readonly Dictionary<T, Gate.Double> _playTimes_Enum = new();

        internal bool CanRegisterNewSound(T eff, float minGap)
        {
            if (minGap <= 0)
                minGap = DEFAULT_GAP;

            var lastPlayed = _playTimes_Enum.GetOrCreate(eff);
            return lastPlayed.IsDirty(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        internal bool TryRegisterNewSoundInstance(T eff, float minGap)
        {
            if (minGap <= 0)
                minGap = DEFAULT_GAP;

            var lastPlayed = _playTimes_Enum.GetOrCreate(eff);
            return lastPlayed.TryChange(Time.realtimeSinceStartup, changeTreshold: minGap);
        }

        internal bool CanPlay(T eff, float minGap) => CanRegisterNewSound(eff, minGap: minGap);
    }
}