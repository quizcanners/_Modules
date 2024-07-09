using QuizCanners.Inspect;
using System;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{

    public static class SpacialEffect
    {
        [Serializable]
        public class MotionDirectionStretch : IPEGI
        {
            [SerializeField] private Transform _stretchRoot;

            Vector3 _previousPosition;
            Vector3 _directionVector;
            public float Size = 1;

            public void Reboot(Transform transform, float size = 1) 
            {
                Size = size;
                _previousPosition = transform.position;
                _directionVector = (transform.position - _previousPosition);
            }

            public void ManagedUpdate(Transform transform) 
            {
                var newDirection = (transform.position - _previousPosition);

                if (newDirection.sqrMagnitude < 0.0001f)
                    return;


                _directionVector = newDirection;
                _previousPosition = transform.position;

                _stretchRoot.position = transform.position - _directionVector.normalized * (1+ Size) * 0.5f;
                if (_directionVector.sqrMagnitude > 0.0001f)
                {
                    _stretchRoot.LookAt(_stretchRoot.position + _directionVector, Vector3.up);
                }

                _stretchRoot.localScale = new Vector3(Size, Size, Size + _directionVector.magnitude);
            }

            void IPEGI.Inspect()
            {
                "Stretch".PegiLabel().Write().Nl();

                "Root".PegiLabel().Edit(ref _stretchRoot).Nl();
               

                pegi.Nl();
            }
        }
    }
}
