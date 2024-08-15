// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Quiz cAnners/Buffer Blit/Copy And Flip"
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
          #pragma vertex my_vert_img
          #pragma fragment frag
          #pragma fragmentoption ARB_precision_hint_fastest
          #include "UnityCG.cginc"

            uniform sampler2D _MainTex;
            uniform float4 _MainTex_TexelSize;

            v2f_img my_vert_img( appdata_img v )
            {
                v2f_img o;
                o.pos = UnityObjectToClipPos (v.vertex);

                o.uv = v.texcoord;

               if (_ProjectionParams.x > 0)
                    o.uv.y = 1 - o.uv.y;

                return o;
            }

            float4 frag(v2f_img i) : COLOR 
            {
            /*
                #ifdef FLIP_NATIVE_TEXTURES
                     i.uv.y = 1-i.uv.y;
                #endif
                */
                
                #if UNITY_UV_STARTS_AT_TOP
                   // if (_MainTex_TexelSize.y < 0)
                            i.uv.y = 1-i.uv.y;
                #endif

      //      if (_ProjectionParams.x < 0)
               // pos.y = 1 - pos.y;

                return tex2D(_MainTex, i.uv);
            }


          ENDCG
        }
    }
      FallBack off
}