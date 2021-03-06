using QuizCanners.Lerp;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class RectTransformTiltMgmt
    {
        private Vector2 tilt;


        public void UpdateTilt(RectTransform rt, Camera cam, bool dontTilt = false, float speed = 100, float mouseEffectRadius = 0.75f, float maxTilt = 25)
        {
            Vector2 targetTilt;

            Vector3 rectPos = RectTransformUtility.WorldToScreenPoint(cam, rt.position).ToVector3();

            speed = Input.GetMouseButton(0) ? speed : speed * 4;

            mouseEffectRadius *= Mathf.Min(Screen.width, Screen.height);

            if (dontTilt || !Input.GetMouseButton(0))
                targetTilt = Vector2.zero;
            else
            {
                float distance = Vector3.Distance(Input.mousePosition, rectPos);

                targetTilt = (Input.mousePosition - rectPos).YX().normalized;

                targetTilt.y = -targetTilt.y;

                targetTilt *= QcMath.SmoothStep(0, mouseEffectRadius, distance) * QcMath.SmoothStep(mouseEffectRadius, 0, distance) * maxTilt;
            }

            targetTilt *= new Vector2(Screen.width, Screen.height) / mouseEffectRadius;

            if (LerpUtils.IsLerpingBySpeed(ref tilt, targetTilt, speed: speed, unscaledTime: true))
            {
                if (tilt.magnitude > maxTilt)
                    tilt = tilt.normalized * maxTilt;

                rt.rotation = Quaternion.Euler(tilt.ToVector3());
            }
        }
    }

}