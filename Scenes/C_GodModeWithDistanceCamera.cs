using QuizCanners.Inspect;
using UnityEngine;


namespace QuizCanners.Utils
{
    public class C_GodModeWithDistanceCamera : Singleton_CameraOperatorConfigurable
    {
        [SerializeField] private Camera _distanceCamera;
        [SerializeField] private float _distantCameraFarClip;

        public override CameraClearFlags ClearFlags
        {
            get => _distanceCamera.clearFlags;
            set => _distanceCamera.clearFlags = value;
        }

        protected override void AdjsutCamera()
        {
            base.AdjsutCamera();
            var cam = MainCam;

            if (!_distanceCamera || !cam)
                return;

            var camTf = _mainCam.transform;
            var disttf = _distanceCamera.transform;

            disttf.position = camTf.position;
            disttf.rotation = camTf.rotation;

            _distanceCamera.nearClipPlane = cam.farClipPlane * 0.5f;
            _distanceCamera.farClipPlane = Mathf.Max( _distantCameraFarClip, cam.farClipPlane*2f);
        }

        public override void Inspect()
        {
            base.Inspect();

            pegi.Nl();
            "Distance camera".PegiLabel().Edit(ref _distanceCamera).Nl();

            "Distant cutoff".PegiLabel().Edit(ref _distantCameraFarClip).Nl();
            if (MainCam) 
            {
                "Main Distance: ".F(MainCam.farClipPlane).PegiLabel().Nl();
            }



        }

    }

    [PEGI_Inspector_Override(typeof(C_GodModeWithDistanceCamera))] internal class GodModeWithDistanceCameraDrawer : PEGI_Inspector_Override { }
}
