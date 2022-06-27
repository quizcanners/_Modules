Shader "Quiz cAnners/Effects/Thin Lines/Straight Additive" 
{
	Properties
	{
		[NoScaleOffset] _MainTex("Albedo (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_Hardness("Softness", Range(0.01,8)) = 0.4
		[KeywordEnum(Horisontal, Vertical)]	_DIR("Direction", Float) = 0
	}

	Category
	{
		Tags
		{
			"Queue" = "Transparent"
			"PreviewType" = "Plane"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Cull Off
		ZWrite Off
			ZTest off
		Blend One One

		SubShader{

			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				#pragma shader_feature_local _DIR_HORISONTAL _DIR_VERTICAL  

				sampler2D _MainTex;
				float4 _Color;
				float _Hardness;
				sampler2D _Global_Noise_Lookup;
				sampler2D _CameraDepthTexture;

				struct v2f 
				{
					float4 pos : SV_POSITION;
					float4 screenPos : TEXCOORD1;                   // v2f (TEXCOORD can be 0,1,2, etc - the obly rule is to avoid duplication)
					float2 texcoord : TEXCOORD2;
					float3 worldPos : TEXCOORD3;
					float4 color: 		COLOR;
				};

				v2f vert(appdata_full v) 
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;
					o.color = v.color * _Color;
					o.screenPos = ComputeScreenPos(o.pos);       	// vert
					COMPUTE_EYEDEPTH(o.screenPos.z);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);

					return o;
				}

				float4 frag(v2f i) : COLOR
				{


					#if _DIR_HORISONTAL
						float2 uv = i.texcoord.xy;
					#elif _DIR_VERTICAL
						float2 uv = i.texcoord.yx;
					#endif

					float2 width = fwidth(uv);

					float2 off = abs(uv.xy - 0.5);

					float brightness = 1; 
					
					float theLine = width.y* _Hardness / (off.y + 0.00001);

					float4 col = i.color;

					brightness *= smoothstep(0.5, 0.25, off.y) * smoothstep(0.5, 0.4 - 10*width, off.x);

					float2 screenUV = i.screenPos.xy / i.screenPos.w;    

					float2 sampleTex = float2(pow(0.5 - off.y, 6) + step(uv.y, 0.5) * 0.25, uv.x);// +screenUV * 0.5;// *(1 / (0.1 + length(width) * 100));

					float4 tex =
						tex2D(_MainTex, sampleTex + float2(uv.x, 0) + float2(_Time.x, - _Time.x * 5))
						* 
						tex2D(_MainTex, sampleTex - float2(uv.x, 0) - float2(_Time.x, -_Time.x * 5))
						;

					//return tex;

					brightness *= 1 + smoothstep(0.05, 0.5, tex.r * brightness);


					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
					float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
					float partZ = i.screenPos.z;
					float differ = sceneZ - partZ;



					float toCamera = length(_WorldSpaceCameraPos - i.worldPos.xyz) - _ProjectionParams.y;

					float fadeLine = smoothstep(0, 1, differ);

					float bloomRange = 1 + toCamera * 0.1;

					float fadeBloom = smoothstep(-bloomRange, bloomRange, differ);


					

					col.a *= (theLine * fadeLine + fadeBloom*0.01 ) * 0.4 * brightness * saturate((toCamera) * 0.4);

					//return col.a;

					col.rgb *=  col.a;

					float3 mix = col.gbr + col.brg;
					col.rgb += mix * mix * 0.05;


	#if USE_NOISE_TEXTURE
					float4 noise = tex2Dlod(_Global_Noise_Lookup, float4(screenUV.xy * 13.5 + float2(_SinTime.w, _CosTime.w) * 32, 0, 0));
	#ifdef UNITY_COLORSPACE_GAMMA
					col.rgb += (noise.rgb - 0.5) * 0.02;
	#else
					col.rgb += (noise.rgb - 0.5) * 0.0075;
	#endif
	#endif

					return col;
				}
			ENDCG

		}
	}
	Fallback "Legacy Shaders/Transparent/VertexLit"
}
}

