Shader "Quiz cAnners/Buffer Blit/Copy Downscale"
{
    Properties{
      _MainTex("Main Texture", 2D) = "black"{}
    }

    SubShader{

        Pass {

          ColorMask RGBA
          Cull Off
          ZTest Always
          ZWrite Off
            Fog { Mode off }

           CGPROGRAM
          #pragma vertex vert_img
          #pragma fragment frag
          #pragma fragmentoption ARB_precision_hint_fastest
          #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            float4 _MainTex_TexelSize;


            float4 GrabMain(float2 uv) {

                float4 tex = tex2Dlod(_MainTex, float4(uv, 0, 0));

                #if UNITY_COLORSPACE_GAMMA
                    tex.rgb *= tex.rgb;
                #endif

                return tex;
            }

            float4 frag(v2f_img i) : COLOR 
            {
                float2 off = _MainTex_TexelSize.xy;

                float4 col = GrabMain(i.uv + off) + GrabMain(i.uv - off);

                off.x = -off.x;

                col += GrabMain(i.uv + off) + GrabMain(i.uv - off);

                col *= 0.25;

                #if UNITY_COLORSPACE_GAMMA
                    col.rgb = sqrt(col.rgb);
                #endif

                return col;
            }


          ENDCG
        }
    }
      FallBack off
}