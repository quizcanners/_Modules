using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace QuizCanners.SpecialEffects
{
    public static partial class Effects
    {
        public class TimeManager : IPEGI, IPEGI_ListInspect
        {
            private const string EFFECT_TIME = "_Effect_Time";
            private const float RESET_VALUE = 30f;

            private static double _effectTimeScaled;
            private readonly Gate.UnityTimeScaled _timeGate = new(Gate.InitialValue.Uninitialized);

            private static double _effectTimeUnScaled;
            private readonly Gate.UnityTimeUnScaled _timeGateUnscaled = new(Gate.InitialValue.Uninitialized);


            private readonly Gate.Frame _frameGate = new();
            private readonly VectorValue _shaderTime = new(EFFECT_TIME);

            [SerializeField] private bool _enabled = true;

            public void OnViewChange()
            {
                if (_effectTimeUnScaled > RESET_VALUE * 0.5f)
                {
                    _effectTimeScaled = 0;
                    _effectTimeUnScaled = 0;
                }
            }

            const float _TIME_SCALE = 0.1f;

            public void ManagedLateUpdate()
            {
                if (_frameGate.TryEnter())
                {
                    if (Application.isPlaying)
                    {
                        _effectTimeScaled += Time.deltaTime * _TIME_SCALE;
                        _effectTimeUnScaled += Time.unscaledDeltaTime * _TIME_SCALE;
                    }
                    else
                    {
                        _effectTimeScaled += _timeGate.GetSecondsDeltaAndUpdate() * _TIME_SCALE;
                        _effectTimeUnScaled += _timeGateUnscaled.GetSecondsDeltaAndUpdate() * _TIME_SCALE;
                    }

                    if (_effectTimeScaled > RESET_VALUE)
                        _effectTimeScaled = 0;

                    if (_effectTimeUnScaled > RESET_VALUE)
                        _effectTimeUnScaled = 0;

                    _shaderTime.SetGlobal(
                        (float)_effectTimeScaled, 
                        (float)_effectTimeUnScaled,
                        (float)_effectTimeScaled % 1,
                        Time.frameCount % 1024
                        //(float)_effectTimeUnScaled % 1
                        );
                }
            }

            public virtual void OnApplicationPauseManaged(bool state)
            {
                _effectTimeScaled = 0;
                _effectTimeUnScaled = 0;
            }


            void IPEGI.Inspect()
            {
                pegi.Nl();

                "Time Value:".PegiLabel().Write_ForCopy(EFFECT_TIME, showCopyButton: true).Nl();

                "Scaled Time (X):".PegiLabel(90).Edit(ref _effectTimeScaled, 0d, (double)RESET_VALUE).Nl();
                "Unscaled Time (Y):".PegiLabel(90).Edit(ref _effectTimeUnScaled, 0d, (double)RESET_VALUE).Nl();

                "Scaled 01 (Z) = {0}".F(_effectTimeScaled % 1).PegiLabel().Nl();
                "UnScaled  01 (W) = {0}".F(_effectTimeUnScaled % 1).PegiLabel().Nl();
            }

            public void InspectInList(ref int edited, int index)
            {
                pegi.ToggleIcon(ref _enabled);

                if ("EffectsTimeManager:  {0}".F(_effectTimeScaled).PegiLabel().ClickLabel() | Icon.Enter.Click())
                    edited = index;
            }
        }
    }
}

