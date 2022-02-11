using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace QuizCanners.SpecialEffects
{
    public class InfiniteParticlesDrawerGUI : PEGI_Inspector_Material
    {
        public const string FadeOutTag = "_FADEOUT";

        public override bool Inspect(Material mat)
        {

            var changed = pegi.ChangeTrackStart();

            var matTok = mat.PegiToken();

            matTok.Edit("SCREENSPACE").Nl();
            matTok.Edit("DYNAMIC_SPEED").Nl();
            matTok.Edit(FadeOutTag).Nl();

            var fo = mat.HasTag(FadeOutTag);

            if (fo)
                "When alpha is one, the graphic will be invisible.".PegiLabel().WriteHint();

            pegi.Nl();

            var dynamicSpeed = mat.GetKeyword("DYNAMIC_SPEED");

            pegi.Nl();

            if (!dynamicSpeed)
                matTok.Edit(speed, "speed", 0, 60).Nl();
            else
            {
                matTok.Edit(time, "Time").Nl();
                "It is expected that time Float will be set via script. Parameter name is _CustomTime. ".PegiLabel().WriteHint();
                pegi.Nl();
            }

            matTok.Edit(tiling, "Tiling", 0.1f, 20f).Nl();

            matTok.Edit(upscale, "Scale", 0.1f, 1).Nl();

            matTok.Edit_Texture("_MainTex").Nl();
            matTok.Edit_Texture("_MainTex2").Nl();
            matTok.Edit(color, "Color fo the Particles").Nl();


            return changed;
        }

        private static readonly ShaderProperty.ColorFloat4Value color = new ShaderProperty.ColorFloat4Value("_Color");
        private static readonly ShaderProperty.FloatValue speed = new ShaderProperty.FloatValue("_Speed");
        private static readonly ShaderProperty.FloatValue time = new ShaderProperty.FloatValue("_CustomTime");
        private static readonly ShaderProperty.FloatValue tiling = new ShaderProperty.FloatValue("_Tiling");
        private static readonly ShaderProperty.FloatValue upscale = new ShaderProperty.FloatValue("_Upscale");

    }
}
