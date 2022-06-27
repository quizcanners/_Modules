using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace QuizCanners.SpecialEffects
{
    public class C_TestMotionBlurScript : MonoBehaviour
    {
        [SerializeField] private RectTransform _rectTransform;

        [SerializeField] private float _speed = 1;
        // Update is called once per frame
        void Update()
        {
            _rectTransform.Rotate(Vector3.forward, _speed * Time.unscaledDeltaTime);
        }

        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
    }
}
