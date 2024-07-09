using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Inspect;

namespace QuizCanners.SpecialEffects
{
    public class UI_InvisibleGraphic : Graphic, IPEGI
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        void IPEGI.Inspect()
        {
            var ico = raycastTarget;
            if ("Raycast Target".PegiLabel().ToggleIcon(ref ico))
                raycastTarget = ico;
        }

    }

    [PEGI_Inspector_Override(typeof(UI_InvisibleGraphic))] internal class UI_InvisibleGraphicDrawer : PEGI_Inspector_Override { }
}