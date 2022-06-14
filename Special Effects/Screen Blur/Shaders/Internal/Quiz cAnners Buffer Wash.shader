Shader "Quiz cAnners/Buffer Blit/Wash"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "black"{}
    }

    SubShader{

        Tags { "Queue" = "Overlay" "RenderType" = "Overlay" }
        LOD 10
        ColorMask RGBA
        Cull Off
        ZTest Always
        ZWrite Off

        Pass
        {

          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
		  #pragma multi_compile ___ USE_NOISE_TEXTURE
          #pragma multi_compile ___ _qcPp_FEED_MOUSE_POSITION
		  #include "UnityCG.cginc"


          struct v2f {
              float4 pos : 		SV_POSITION;
              float2 texcoord : 	TEXCOORD1;
              float2 noiseUV :	TEXCOORD2;
            };

            v2f vert(appdata_full v) {
              v2f o;
              o.pos = UnityObjectToClipPos(v.vertex);
              o.texcoord = v.texcoord;
              o.noiseUV = o.texcoord.xy * (123.12345678) + float2(_SinTime.x, _CosTime.y) * 32.12345612;

              return o;
            }

            sampler2D _MainTex;
			uniform sampler2D _Global_Noise_Lookup;
            uniform float2 _qcPp_MousePosition;
            float _qcPp_Screen_Blur_Iteration;

            float4 frag(v2f o) : SV_TARGET{

                float2 uv = o.texcoord.xy;



#if USE_NOISE_TEXTURE
                const float _BlurEffectPortion = 0.002;
                float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(o.noiseUV, 0, 0)) -0.5;
#else
                const float _BlurEffectPortion = 0.002;
				float4 noise = 0.5;
#endif

                float4 sum = 0;

                float relation = _ScreenParams.x / _ScreenParams.y;

                float ker = (1 + _qcPp_Screen_Blur_Iteration) * 0.1;

                float xker = (1 * noise.b)*_BlurEffectPortion * ker;


#if _qcPp_FEED_MOUSE_POSITION
                uv += (_qcPp_MousePosition - uv) * 0.005;

               _BlurEffectPortion *= 0.1;
#endif

   
                #define GRABPIXELX(weight,kernel) tex2Dlod( _MainTex, float4(uv + float2(kernel*xker, 0)  ,0,0)) * weight

                sum += GRABPIXELX(0.15, -2.0);
                sum += GRABPIXELX(0.30, -1.0);
                sum += GRABPIXELX(0.10, 0.0);
                sum += GRABPIXELX(0.30, +1.0);
                sum += GRABPIXELX(0.15, +2.0);

                float yker = (1* noise.g)*_BlurEffectPortion * relation * ker;

                #define GRABPIXELY(weight,kernel) tex2Dlod( _MainTex, float4(uv + float2(0, kernel*yker)  ,0,0)) * weight

                sum += GRABPIXELY(0.15, -2.0);
                sum += GRABPIXELY(0.30, -1.0);
                sum += GRABPIXELY(0.10, 0.0);
                sum += GRABPIXELY(0.30, +1.0);
                sum += GRABPIXELY(0.15, +2.0);

                sum.rgb *= 0.5;

                return sum;
            }
             ENDCG
        }
    }
      FallBack Off
}
