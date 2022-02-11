using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace QuizCanners.SpecialEffects
{
    partial class Singleton_SpecialEffectShaders
    {

        public class EffectsTimeManager : IPEGI, IPEGI_ListInspect
        {
            private const string EFFECT_TIME = "_Effect_Time";
            private const float RESET_VALUE = 30f;

            private static double _effectTime;
            private readonly FloatValue _shaderTime = new FloatValue(EFFECT_TIME);
            private readonly Gate.Frame _frameGate = new Gate.Frame();
            private readonly Gate.Time _timeGate = new Gate.Time();

            [SerializeField] private bool _enabled = true;

            public void OnViewChange()
            {
                if (_effectTime > RESET_VALUE * 0.5f)
                    _effectTime = 0;
            }

            const float _TIME_SCALE = 0.1f;

            public void ManagedLateUpdate()
            {
                if (_frameGate.TryEnter())
                {
                    if (Application.isPlaying)
                    {
                        _effectTime += Time.deltaTime * _TIME_SCALE;
                    }
                    else
                    {
                        _effectTime += _timeGate.GetSecondsDeltaAndUpdate() * _TIME_SCALE;
                    }

                    if (_effectTime > RESET_VALUE)
                        _effectTime = 0;

                    _shaderTime.SetGlobal((float)_effectTime);
                }
            }

            public void OnApplicationPauseManaged(bool state)
            {
                _effectTime = 0;
            }


            public void Inspect()
            {
                pegi.Nl();

                "Time Value:".PegiLabel().Write_ForCopy(EFFECT_TIME, showCopyButton: true).Nl();

                "Current:".PegiLabel(90).Edit(ref _effectTime, 0d, (double)RESET_VALUE).Nl();
            }

            public void InspectInList(ref int edited, int index)
            {
                pegi.ToggleIcon(ref _enabled);

                if ("EffectsTimeManager:  {0}".F(_effectTime).PegiLabel().ClickLabel() | Icon.Enter.Click())
                    edited = index;
            }
        }
    }
}

