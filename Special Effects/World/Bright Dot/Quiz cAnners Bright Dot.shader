Shader "Quiz cAnners/Effects/Bright Spot" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_Hardness("Hardness", Range(0.1,16)) = 1
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}

	Category{
		Tags{
			"Queue" = "Transparent"
			"PreviewType" = "Plane"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Cull Off
		ZWrite Off
		Blend SrcAlpha One

		SubShader{

			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0
				#pragma multi_compile ___ USE_NOISE_TEXTURE


				float4 _Color;
				float _Hardness;
				sampler2D _Global_Noise_Lookup;
				sampler2D _CameraDepthTexture;
				float _InvFade;

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float4 screenPos : TEXCOORD1;
				};

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);

					o.pos = UnityObjectToClipPos(v.vertex);

					o.texcoord = v.texcoord.xy;

					o.screenPos = ComputeScreenPos(o.pos);
					COMPUTE_EYEDEPTH(o.screenPos.z);

					return o;
				}

				float4 frag(v2f i) : COLOR
				{

					float2 screenUV = i.screenPos.xy / i.screenPos.w;

					float2 off = i.texcoord - 0.5;
					off *= off;
					float len = (off.x + off.y) * 2;

					float diff = length(fwidth(i.texcoord)) * 3;

					float alpha = diff * 10  / (0.001 +  len*100 * _Hardness) * smoothstep(0.5, 0, len);

					float4 col = _Color;

					col.a = 1;

#                   if USE_NOISE_TEXTURE
						float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(i.texcoord.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
						col.rgb += (noise.rgb - 0.5) * 0.4 * col.rgb;
#                   endif

						float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
						float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
						float partZ = i.screenPos.z;
						float fade = smoothstep(0, 1, _InvFade * (sceneZ - partZ));


					col.rgb *= alpha * fade;

					float3 mix = col.gbr + col.brg;
					col.rgb += mix * mix * 0.05;

					return col;
				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

