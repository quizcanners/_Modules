using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace QuizCanners.SpecialEffects
{
    internal static class ShaderTags
    {
        internal static readonly ShaderTag PixelPerfectUi = new("PixelPerfectUI");

        internal static class PixelPerfectUis
        {
            public static readonly ShaderTagValue Simple = new("Simple", PixelPerfectUi);
            public static readonly ShaderTagValue Position = new("Position", PixelPerfectUi);
            public static readonly ShaderTagValue AtlasedPosition = new("AtlasedPosition", PixelPerfectUi);
            public static readonly ShaderTagValue FadePosition = new("FadePosition", PixelPerfectUi);
        }

        internal static readonly ShaderTag SpriteRole = new("SpriteRole");

        internal static class SpriteRoles
        {
            public static readonly ShaderTagValue Hide = new("Hide", SpriteRole);
            public static readonly ShaderTagValue Tile = new("Tile", SpriteRole);
            public static readonly ShaderTagValue Normal = new("Normal", SpriteRole);
        }

        internal static readonly ShaderTag PerEdgeData = new("PerEdgeData");

        internal static class PerEdgeRoles
        {
            public static readonly ShaderTagValue UnlinkedCourners = new("Unlinked", PerEdgeData);
            public static readonly ShaderTagValue LinkedCourners = new("Linked", PerEdgeData);
        }
    }

    internal static class ProceduralUiExtensions
    {
        public static void AddFull(this VertexHelper vh, UIVertex vert) =>
#if UNITY_2019_1_OR_NEWER
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.uv2, vert.uv3, vert.normal, vert.tangent);
#else
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.normal, vert.tangent);
#endif

#if UNITY_EDITOR

        [UnityEditor.MenuItem("GameObject/UI/" + QcUtils.QUIZCANNERS + "/Invisible Raycat Target", false, 0)]
        private static void CreateInvisibleRaycastTarget()
        {
            var els = QcUnity.CreateUiElement<UI_InvisibleGraphic>(UnityEditor.Selection.gameObjects);

            foreach (var el in els)
            {
                el.name = "[]";
                if (!el.gameObject.GetComponent<CanvasRenderer>())
                    el.gameObject.AddComponent<CanvasRenderer>();
            }
        }

        [UnityEditor.MenuItem("GameObject/UI/" + QcUtils.QUIZCANNERS + "/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement()
        {
            QcUnity.CreateUiElement<RoundedGraphic>(UnityEditor.Selection.gameObjects, onCreate: el =>
            {
                el.maskable = el.GetComponentInParent<Mask>();
                el.raycastTarget = false;
                if (!el.gameObject.GetComponent<CanvasRenderer>())
                    el.gameObject.AddComponent<CanvasRenderer>();

            });
        }
#endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY)
        {
            vertex.uv0 = new Vector2(uvX, uvY);
            vertex.position = new Vector2(posX.x, posY.y);
            return vertex;
        }
    }

    internal class PixelPerfectMaterialDrawer : PEGI_Inspector_Material
    {
        private static readonly ShaderProperty.FloatValue Softness = new(RoundedGraphic.EDGE_SOFTNESS_FLOAT);

        private static readonly ShaderProperty.TextureValue Outline = new("_OutlineGradient");

        public override bool Inspect(Material mat)
        {
            var changed = pegi.Toggle_DefaultInspector(mat);

            mat.PegiToken().Edit(Softness, "Softness", 0, 1).Nl();
            mat.PegiToken().Edit(Outline).Nl();

            if (mat.IsKeywordEnabled(RoundedGraphic.UNLINKED_VERTICES))
                "UNLINKED VERTICES".PegiLabel().Nl();

            var go = QcUnity.GetFocusedGameObject();

            if (go)
            {
                var rndd = go.GetComponent<RoundedGraphic>();

                if (!rndd)
                    "No RoundedGrahic.cs detected, shader needs custom data.".PegiLabel().WriteWarning();
                else if (!rndd.enabled)
                    "Controller is disabled".PegiLabel().WriteWarning();
            }

            return changed;
        }
    }
}

