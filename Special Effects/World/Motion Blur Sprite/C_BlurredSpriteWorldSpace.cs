using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class C_BlurredSpriteWorldSpace : MonoBehaviour, IPEGI, INeedAttention
    {
        [SerializeField] private MeshRenderer _renderer;
        [SerializeField] private bool _trackMotion = true;
        [SerializeField] private bool _rotateToCamera = true;
        [SerializeField] private bool _autoscale = true;

        private readonly ShaderProperty.FloatValue ROTATION = new("_Angle");
        private readonly ShaderProperty.FloatValue STRENGTH = new("_Force");
        private readonly ShaderProperty.FloatValue VISIBILITY = new("_Visibility");

        public bool SeenByCamera = true;

        Vector2 _previousPosition;
        Vector2 previousDiff;
        private MaterialPropertyBlock _block;
        private readonly LogicWrappers.Request _meshDataDirty = new();

        void OnBecameVisible()
        {
            SeenByCamera = true;
        }

        void OnBecameInvisible()
        {
            SeenByCamera = false;
        }

        public float Visibility 
        {
            get => VISIBILITY.latestValue;
            set 
            {
                VISIBILITY.SetOn(_block, Mathf.Clamp01(value));
                _meshDataDirty.CreateRequest();
            }
        }


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
                value = Mathf.Clamp01(value);
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

        public void Hide() 
        {
            Visibility = 0;
            _renderer.SetPropertyBlock(_block);

        }

        void OnEnable() 
        {
            _block ??= new MaterialPropertyBlock();

            VectorToBlur(Vector2.zero);
        }

        void LateUpdate() 
        {
            var inst = Singleton.Get<Singleton_CameraOperatorGodMode>(); //C_UiCameraForEffectsManagement.Camera;

            if (!inst)
                return;

            transform.LookAt(inst.MainCam.transform);

            if (_autoscale)
            {
                transform.localScale = 0.08f * Vector3.Distance(transform.position, inst.transform.position) * Vector3.one;
            }

            if (_trackMotion)
            {
                TrackMotion();

                void TrackMotion()
                {
                    var pos = inst.MainCam.WorldToViewportPoint(transform.position).XY();

                    if (pos.x < 0 || pos.x > 1 || pos.y < 0 || pos.y > 1)
                    {
                        return;
                    }

                    /*
                    if (_previousPosition == pos)
                    {
                        if (previousDiff != Vector2.zero)
                        {
                            previousDiff = Vector2.zero;
                            VectorToBlur(Vector2.zero);
                        }

                        return;
                    }*/

                    pos.x = 1 - pos.x;

                    var diff =  _previousPosition - pos;

                    var smoothedDiff = (diff + previousDiff) * 0.5f;
                    VectorToBlur(smoothedDiff / (Time.deltaTime + 0.001f));
                    previousDiff = diff;
                    _previousPosition = pos;
                }
            }

            if (_meshDataDirty.TryUseRequest())
                _renderer.SetPropertyBlock(_block);

        }

        void IPEGI.Inspect()
        {
            pegi.Nl();

            var changes = pegi.ChangeTrackStart();

            _block ??= new MaterialPropertyBlock();

            var vis = Visibility;
            if ("Visibility".PegiLabel().Edit_01(ref vis).Nl())
                Visibility = vis;

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

        public string NeedAttention()
        {
           // if (Application.isPlaying && !C_UiCameraForEffectsManagement.Camera)
             //   return "NO Ui Camera";

            return null;
        }
    }

    [PEGI_Inspector_Override(typeof(C_BlurredSpriteWorldSpace))] internal class C_BlurredSpriteWorldSpaceDrawer : PEGI_Inspector_Override { }
}