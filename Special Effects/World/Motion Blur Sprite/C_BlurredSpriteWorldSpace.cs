using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class C_BlurredSpriteWorldSpace : MonoBehaviour, IPEGI
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private bool _trackMotion = true;
        [SerializeField] private bool _rotateToCamera = true;

        private readonly ShaderProperty.FloatValue ROTATION = new("_Angle");
        private readonly ShaderProperty.FloatValue STRENGTH = new("_Force");

        Vector2 _previousPosition;
        Vector2 previousDiff;
        private MaterialPropertyBlock _block;
        private readonly LogicWrappers.Request _meshDataDirty = new();
  


        float Rotation01
        {
            get => ROTATION.latestValue;
            set
            {
                ROTATION.SetOn(_block, value);
                _meshDataDirty.CreateRequest();
            }
        }

        float Strength
        {
            get => STRENGTH.latestValue;
            set
            {
                STRENGTH.SetOn(_block, value); 
                _meshDataDirty.CreateRequest();
            }
        }

        float GetAngle(Vector2 vec)
        {
            float angle = Mathf.Atan2(vec.x, vec.y) + Mathf.PI * 0.5f;
            return angle;
        }

        void VectorToBlur(Vector2 vec)
        {
            float angle = GetAngle(vec);
            Rotation01 = angle;
            Strength = vec.magnitude;
        }

        void OnEnable() 
        {
            _block ??= new MaterialPropertyBlock();

            VectorToBlur(Vector2.zero);
        }

        void LateUpdate() 
        {
            transform.LookAt(Camera.main.transform);

            if (_trackMotion)
            {
                TrackMotion();

                void TrackMotion()
                {
                    var pos = Camera.main.WorldToViewportPoint(transform.position).XY();

                    if (pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1)
                    {
                        return;
                    }

                    if (_previousPosition == pos)
                    {
                        if (previousDiff != Vector2.zero)
                        {
                            previousDiff = Vector2.zero;
                            VectorToBlur(Vector2.zero);
                        }

                        return;
                    }

                    pos.x = 1 - pos.x;

                    var diff =  _previousPosition - pos;
                    previousDiff = diff; // (previousDiff * 2f + diff) * 0.3333f;
                    VectorToBlur(previousDiff / (Time.deltaTime + 0.01f));
                    _previousPosition = pos;
                }
            }

            if (_meshDataDirty.TryUseRequest())
                _renderer.SetPropertyBlock(_block);

        }

        public void Inspect()
        {
            pegi.Nl();

            var changes = pegi.ChangeTrackStart();

            _block ??= new MaterialPropertyBlock();

            "Rotate to camera".PegiLabel().ToggleIcon(ref _rotateToCamera).Nl();
            "Track Motion".PegiLabel().ToggleIcon(ref _trackMotion).Nl();
            "Mesh Renderer".PegiLabel(90).Edit_IfNull(ref _renderer, gameObject).Nl();

            if (!_trackMotion)
            {
                var rot = Rotation01;
                "Rotation".PegiLabel(50).Edit(ref rot, 0, Mathf.PI * 2).Nl()
                    .OnChanged(() => Rotation01 = rot);

                var str = Strength;
                "Strength".PegiLabel().Edit_01(ref str).Nl()
                    .OnChanged(() => Strength = str);
            } else 
            {
                "Strength: {0}".F(Strength).PegiLabel().Nl();
            }

            if (changes) 
            {
                _renderer.SetPropertyBlock(_block);
            }
        }


        void Reset()
        {
            _renderer = GetComponent<MeshRenderer>();
        }
    }

    [PEGI_Inspector_Override(typeof(C_BlurredSpriteWorldSpace))] internal class C_BlurredSpriteWorldSpaceDrawer : PEGI_Inspector_Override { }
}