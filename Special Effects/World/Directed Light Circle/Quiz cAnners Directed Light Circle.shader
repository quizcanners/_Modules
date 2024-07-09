Shader "Quiz cAnners/Effects/Directed Light" 
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1,1,1,1)
		[HDR]_FadeColor("Fade Color", Color) = (1,1,1,1)
        _Angle("Angle 01", Range(0.01,0.99)) = 0.5
	}

	Category
	{
		Tags
		{
			"Queue" = "Transparent+1"
			"PreviewType" = "Quad"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}

		Cull Back
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader
		{
			Pass
			{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma multi_compile ___ qc_LAYARED_FOG
				#include "Assets/Qc_Rendering/Shaders/Savage_Sampler_Transparent.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0


				struct v2f 
				{
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float4 screenPos : TEXCOORD1;
					float3 viewDir		: TEXCOORD2;
					float3 normal		: TEXCOORD3;
					float3 worldPos : TEXCOORD4;
				};

				v2f vert(appdata_full v) 
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					
					o.screenPos = ComputeScreenPos(o.pos); 
					o.texcoord = v.texcoord.xy - 0.5;
					o.viewDir = WorldSpaceViewDir(v.vertex);
					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
					COMPUTE_EYEDEPTH(o.screenPos.z);
					 o.worldPos = mul(unity_ObjectToWorld, v.vertex);

					return o;
				}

				float4 _Color;
				float4 _FadeColor;
				float _Angle;

	
				float4 frag(v2f i) : COLOR
				{
					float3 viewDir = normalize(i.viewDir.xyz);

					float alpha = dot(i.texcoord,i.texcoord) * 4;


					float alphaClamp = saturate(1-alpha);
					float alphaDist = 1/pow(alpha + 1,8);
					alpha = alphaClamp * alphaDist;



					float2 screenUV = i.screenPos.xy / i.screenPos.w; 
					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
					float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
					float partZ = i.screenPos.z;
					float fade = smoothstep(-2, -0.5, sceneZ - partZ);



					alpha *= fade;

					float dott = dot(viewDir, i.normal);

					float fresnel = smoothstep(_Angle, 1, dott);

					alpha *= fresnel;

					float4 col = lerp(_FadeColor, _Color, alpha);

					col.a *= alpha;

					float3 mix = (col.gbr + col.brg)*alpha;
					col.rgb += mix * mix * 0.1;

					col = max(0, col);

					ApplyLayeredFog_Transparent(col, screenUV, i.worldPos);

					return col;

				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

