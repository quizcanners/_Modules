using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class Singleton_ScreenBlurBackgroundController : Singleton.BehaniourBase
    {
        [SerializeField] protected Camera myCamera;
        public override string InspectedCategory => Singleton.Categories.RENDERING;

        private void Reset()
        {
            myCamera = GetComponent<Camera>();
        }

        internal bool IsSetupAndEnabled => myCamera && this.isActiveAndEnabled;
        
        internal void RenderTo(RenderTexture tex) 
        {
            if (!myCamera) 
            {
                Debug.LogError("Camera not set on {0}".F(nameof(Singleton_ScreenBlurBackgroundController)), this);
                return;
            }

            myCamera.enabled = false;
            myCamera.targetTexture = tex;

            myCamera.Render();
            myCamera.targetTexture = null;
            myCamera.enabled = true;
        }


        #region Inspector

        public override void Inspect() 
        {
            "Camera".PegiLabel(60).Edit_IfNull(ref myCamera, gameObject).Nl();

            "Attach this to Camera that only renders Background. And use ScreenBlurController to request screen shot update with background."
                .PegiLabel().Write_Hint();
        }

        #endregion

    }

    [PEGI_Inspector_Override(typeof(Singleton_ScreenBlurBackgroundController))]
    internal class ScreenBlurBackgroundControllerDrawer : PEGI_Inspector_Override { }
}