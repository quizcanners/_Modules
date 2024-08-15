using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class C_UiCameraForEffectsManagement : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        public static C_UiCameraForEffectsManagement Instance;

        public static Camera UiCameraOrMain => Instance && Instance._camera ? Instance._camera : Camera.main;

        private void OnEnable()
        {
            if (Instance)
                gameObject.DestroyWhatever();
            else 
                Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Reset()
        {
            _camera = GetComponent<Camera>();
        }

#if UNITY_EDITOR
        private readonly ShaderProperty.Feature _isInEditor = new("_qc_SCENE_RENDERING");

        void OnPreRender() => _isInEditor.Enabled = false;
        void OnPostRender() => _isInEditor.Enabled = true;
        #endif
    }
}
