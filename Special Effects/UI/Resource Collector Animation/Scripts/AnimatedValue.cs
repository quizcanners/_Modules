using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public static class AnimatedValue
    {
        public abstract class Base : IPEGI
        {
            [SerializeField] protected bool _initialSet;
            public bool AnimationIsAdditive { get; protected set; }

            public abstract void Inspect();
        }

        public abstract class Generic<T> : Base
        {
            protected T currentValue;
            protected T targetValue;
            protected T valueChangeSpeed;

            [SerializeField] protected readonly Gate.Frame _update = new Gate.Frame();
            [SerializeField] protected readonly Gate.Frame _animation = new Gate.Frame();
        }

        public class Double : Generic<double>
        {
            const int MAX_SECONDS_TO_ANIMATE = 2;
            const float MIN_TICK = 1;
            const float MIN_VALUE_CHANGE_SPEED = 10;

            public double GetWithoutUpdating() => currentValue;

            public double UpdateAndGet()
            {
                CheckAnimation();
                return currentValue;
            }

            private void OnValueChanged()
            {
                _update.DoneThisFrame = true;
                _animation.DoneThisFrame = true;
            }

            public void SetCurrentValue(double value)
            {
                currentValue = value;
                OnValueChanged();
            }

            public double TargetValue
            {
                get => targetValue;
                set
                {
                    if (!_initialSet)
                    {
                        _initialSet = true;
                        SetCurrentValue(value);
                        targetValue = value;
                        return;
                    }

                    if (targetValue == value)
                        return;

                    targetValue = value;

                    valueChangeSpeed = Math.Max(valueChangeSpeed, Math.Max(MIN_VALUE_CHANGE_SPEED, Math.Abs(targetValue - currentValue) / MAX_SECONDS_TO_ANIMATE));
                }
            }

            public bool IsAnimatingThisFrame
            {
                get
                {
                    CheckAnimation();
                    return _animation.DoneThisFrame;
                }
            }

            private void CheckAnimation()
            {
                if (!_initialSet || _update.DoneThisFrame)
                    return;

                _update.DoneThisFrame = true;

                var diff = targetValue - currentValue;

                var absDiff = Math.Abs(diff);

                if (absDiff == 0)
                {
                    valueChangeSpeed = 0;
                    return;
                }

                AnimationIsAdditive = diff > 0;

                _animation.DoneThisFrame = true;

                if (absDiff < MIN_TICK)
                {
                    currentValue = targetValue;
                    valueChangeSpeed = 0;
                }
                else
                {
                    currentValue = LerpUtils.LerpBySpeed(currentValue, targetValue, valueChangeSpeed, unscaledTime: true);
                }
            }

            public override void Inspect()
            {
                "Current".PegiLabel().Edit(ref currentValue).Nl();

                var tv = TargetValue;
                "Target".PegiLabel().Edit(ref tv).Nl(() => TargetValue = tv);
            }

            public override string ToString() => currentValue == targetValue
                ? currentValue.ToReadableString()
                : "{0}=>{1}".F(currentValue.ToReadableString(), targetValue.ToReadableString());

        }
    }
}