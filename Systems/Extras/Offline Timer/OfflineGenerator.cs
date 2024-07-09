using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace QuizCanners.SavageTurret
{
    [Serializable]
    public class OfflineGenerator : IPEGI
    {
        [SerializeField] private SerializableDateTime _lastSyncDateTime;

        [SerializeField] private SerializableTimeSpan _unprocessedSynchronizedTime;
        [SerializeField] private SerializableTimeSpan _processedWithoutSyncronization;
        [NonSerialized] private TimeSpan _unprocessedActiveTime;

        [NonSerialized] private readonly Gate.Frame _frameGate = new();
        [NonSerialized] private readonly Gate.SystemTime _editorOnlyTimeGate = new(Gate.InitialValue.Uninitialized);

        private Singleton_NetworkTime NetworkTime => Singleton.Get<Singleton_NetworkTime>();

        public TimeSpan UnProcessedTimeDelta
        {
            get
            {
                if (_frameGate.TryEnter())
                    _unprocessedActiveTime += TimeSpan.FromSeconds(_editorOnlyTimeGate.GetSecondsDeltaAndUpdate());

                return _unprocessedActiveTime + _unprocessedSynchronizedTime.Value;
            }
        }

        public void CheatSkipTime(TimeSpan span) => _unprocessedSynchronizedTime += span;

        public void FeedSynchronizationTime(DateTime syncDateTime)
        {
            if (!_lastSyncDateTime.IsSet)
            {
                _lastSyncDateTime = syncDateTime;
                _processedWithoutSyncronization = TimeSpan.Zero;
                return;
            }

            TimeSpan syncSpan = syncDateTime - _lastSyncDateTime;
            _lastSyncDateTime = syncDateTime;

            if (syncSpan.TotalSeconds < 0) 
            {
                Debug.LogError("Offline Time passed < 0");
                return;
            }

            SubtractOvelap(ref syncSpan, ref _processedWithoutSyncronization);
            _unprocessedSynchronizedTime += syncSpan;
            _unprocessedActiveTime = TimeSpan.Zero; // Unprocessed active time is handled by sync

            if (_unprocessedSynchronizedTime < TimeSpan.Zero) 
            {
                Debug.LogError("Time Span was below zero upon sync");
                _unprocessedSynchronizedTime = TimeSpan.Zero;
            }

            if (_processedWithoutSyncronization.Value.TotalSeconds > 60) 
            {
                Debug.LogError("Leftover Offline Generation: {0}".F(_processedWithoutSyncronization.Value.ToShortDisplayString()));
                _processedWithoutSyncronization.Value = TimeSpan.Zero;
            }
        }

        public void OnDeltaTimeProcessed() 
        {
            _processedWithoutSyncronization += _unprocessedActiveTime;
            _unprocessedActiveTime = TimeSpan.Zero;
            _unprocessedSynchronizedTime.Value = TimeSpan.Zero;
        }

        public void OnDeltaTimeProcessed(TimeSpan processed)
        {
            SubtractOvelap(ref processed, ref _unprocessedSynchronizedTime);
            _processedWithoutSyncronization += processed;
            SubtractOvelap(ref _unprocessedActiveTime, ref processed);
            if (processed > TimeSpan.Zero)
            {
                Debug.LogError("Processed time was above the unprocessed by "+ (-processed).ToShortDisplayString());
            }
        }

        private void SubtractOvelap(ref TimeSpan a, ref TimeSpan b) 
        {
            TimeSpan overlap = (a < b) ? a : b;
            a -= overlap;
            b -= overlap;
        }

        private void SubtractOvelap(ref TimeSpan a, ref SerializableTimeSpan b)
        {
            TimeSpan overlap = (a < b) ? a : b;
            a -= overlap;
            b -= overlap;
        }

        private void ResetAll()
        {
            _lastSyncDateTime = new SerializableDateTime();
            _unprocessedSynchronizedTime = new SerializableTimeSpan();
            _processedWithoutSyncronization = TimeSpan.Zero;
            _unprocessedActiveTime = new TimeSpan();
        }

        #region Inspector
        void IPEGI.Inspect()
        {
            if ("Reset All".PegiLabel(toolTip: "Will loose all generated resource").ClickConfirm(confirmationTag: "Res All").Nl())
                ResetAll();

            if (NetworkTime != null)
                NetworkTime.Nested_Inspect();
            else
            {
                "{0} service is missing".F(nameof(Singleton_NetworkTime)).PegiLabel().Write_Hint();
                "Feed System Time".PegiLabel().Click(() =>
                    FeedSynchronizationTime(DateTime.Now));
            }
            pegi.Nl();

            if (!_lastSyncDateTime.IsSet)
                "NOT SYNCHRONIZED".PegiLabel().Nl();

            if (NetworkTime != null)
            {
                if (!NetworkTime.IsTimeValid)
                    "Network Time Currently Unknown".PegiLabel().Write();
                else
                {
                    "(Network_Time - SyncTime): {0} ".F((NetworkTime.Time - _lastSyncDateTime).ToShortDisplayString()).PegiLabel().Write();
                    "Feed".PegiLabel().Click(()=> FeedSynchronizationTime(NetworkTime.Time));
                }
            }
            pegi.Nl();

            "Offline Synchronized: {0}".F(_unprocessedSynchronizedTime.Value.ToShortDisplayString()).PegiLabel().Nl();
            "Active Time: {0}".F(_unprocessedActiveTime.ToShortDisplayString()).PegiLabel().Nl();
            "Get Total UnProcessed Time: {0}".F(UnProcessedTimeDelta.ToShortDisplayString()).PegiLabel().Nl();


            "Process".PegiLabel().Click(OnDeltaTimeProcessed).Nl();

            Icon.Clear.Click(() => _processedWithoutSyncronization.Value = TimeSpan.Zero);

            "Processed UnSynchronized : {0}".F(_processedWithoutSyncronization.Value.ToShortDisplayString()).PegiLabel().Nl();

            "Cheat Add 30 min".PegiLabel().Click(() => CheatSkipTime(TimeSpan.FromMinutes(30))); 

            //_testTimer.Nested_Inspect();
        }

        #endregion
    }
}