using QuizCanners.Inspect;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.SpecialEffects
{
    public class C_MotionBlurFeed : Image, IPEGI
    {
        //private MaterialInstancer.ForUiGraphics instancer;

        [SerializeField] private bool _trackMotion = true;

        Vector2 previousPosition;
        Vector2 previousDiff;

        float Rotation01;


        float Strength;


        float GetAngle(Vector2 vec)
        {
            float angle = Mathf.Atan2(vec.x, vec.y) + Mathf.PI * 0.5f;
            return angle;
        }
        void VectorToBlur(Vector2 vec) 
        {
            float angle = GetAngle(vec);
            Rotation01 = angle;
            Strength = vec.magnitude / Mathf.Max(Screen.width, Screen.height);
            SetAllDirty();
        }

        protected override void Start() 
        {
            base.Start();
            VectorToBlur(Vector2.zero);
        }

        void Update()
        {
            if (_trackMotion)
            {
                var pos = RectTransformUtility.WorldToScreenPoint(C_UiCameraForEffectsManagement.Camera, transform.position);

                if (previousPosition == pos)
                {
                    if (previousDiff != Vector2.zero)
                    {
                        previousDiff = Vector2.zero;
                        VectorToBlur(Vector2.zero);
                    }

                    return;
                }

                var diff = previousPosition - pos;
              //  previousDiff = (previousDiff * 2f + diff) * 0.3333f;
                VectorToBlur(diff / (Time.unscaledDeltaTime + 0.01f));
                previousPosition = pos;
            }
        }


        Vector2 testVector;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            var oldList = new List<UIVertex>();
            vh.GetUIVertexStream(oldList);

            vh.Clear();

            for (int i=0; i<oldList.Count; i++) 
            {
                UIVertex v = oldList[i];
                v.uv1 = new Vector4(Strength, Rotation01, 0,0);
                vh.AddFull(v);
            }

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(3, 4, 5);


            //Debug.Log("{0} vertexes".F(oldList.Count));

           
            //vh.Clear();
           // UIVertex vertex = UIVertex.simpleVert;
        }

        public void Inspect()
        {
            pegi.Nl();

            "Sprite".PegiLabel(90).Edit_Property(() => sprite, this).Nl();

            "Material".PegiLabel(90).Edit_Property(() => material, this).Nl();
            "Color".PegiLabel(90).Edit_Property(() => color, this).Nl();
            "Maskable".PegiLabel(90).Edit_Property(() => maskable, this, includeChildren: true).Nl();
            "Raycast Target".PegiLabel(90).Edit_Property(() => raycastTarget, this).Nl();

            "Track Motion".PegiLabel().ToggleIcon(ref _trackMotion).Nl(SetAllDirty);

            if (!C_UiCameraForEffectsManagement.Camera)
                "{0} not found. Will Use Camera.main".F(nameof(C_UiCameraForEffectsManagement)).PegiLabel().Nl();

            if (!_trackMotion)
            {
                var changes = pegi.ChangeTrackStart();

                var rot = Rotation01;
                "Rotation".PegiLabel(50).Edit(ref rot, 0, Mathf.PI * 2).Nl()
                    .OnChanged(() => Rotation01 = rot);

                var str = Strength;
                "Strength".PegiLabel().Edit_01(ref str).Nl()
                    .OnChanged(() => Strength = str);

                "Test Vector".PegiLabel().Edit(ref testVector).Nl()
                    .OnChanged(() => VectorToBlur(testVector));


                var ang = GetAngle(testVector); //.Angle();
                "Angle Degrees: {0}".F(ang).PegiLabel().Nl();

                if (changes)
                    SetAllDirty();
            }
            else 
            {
                "Strength: {0}".F(Strength).PegiLabel().Nl();
            }

            if (sprite) 
            {
                "For the best effect make sure the Sprite has MipMaps with Border Mip Maps Enabled".PegiLabel().Write_Hint();
            }
           // if (Application.isPlaying && instancer!= null)
              //  instancer.Nested_Inspect();
        }
    }

    [PEGI_Inspector_Override(typeof(C_MotionBlurFeed))] internal class C_MotionBlurFeedDrawer : PEGI_Inspector_Override { }
}
