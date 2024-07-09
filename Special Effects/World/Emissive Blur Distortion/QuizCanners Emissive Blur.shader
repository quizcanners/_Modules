Shader "QcRendering/Effect/Emissive Blur Screen Grab"
{
	Properties
	{
		[HDR] _TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Tex (Feed UV, UV2, AnimBlend)", 2D) = "white" {}
		_InvFade("Soft Particles Factor", Range(0.01,5)) = 1.0
		_FadeRange("Fade When Near", Range(0.1,100)) = 0.3
	}

	Category
	{
		SubShader
		{
			GrabPass
			{
				"_GrabTexture"
			}


			Tags
			{ 
				"Queue" = "Transparent+10"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"LightMode" = "ForwardBase"
			}

			Blend SrcAlpha OneMinusSrcAlpha

			Cull Off
			ZWrite Off

			Pass
			{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag

				#include "Assets/Qc_Rendering/Shaders/Savage_Sampler_Standard.cginc"


				sampler2D _MainTex;
				fixed4 _TintColor;

				struct appdata_t 
				{
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float3 normal : NORMAL;
					float4 texcoords : TEXCOORD0;
					float texcoordBlend : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					float2 texcoord2 : TEXCOORD1;
					float blend : TEXCOORD2;
					float4 screenPos : TEXCOORD3;
					float3 worldPos	: TEXCOORD4;
					float3 viewDir	: TEXCOORD5;
					float4 uvgrab : TEXCOORD7;
					  float3 normal		: TEXCOORD8;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				float4 _MainTex_ST;
				sampler2D _MotionVectorsMap;
				float _FlowIntensity;
				float _GridSize_Col;
				float _GridSize_Row;

				UNITY_DECLARE_SCREENSPACE_TEXTURE(_GrabTexture);

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v); //Insert
					UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.screenPos = ComputeScreenPos(o.vertex);
					COMPUTE_EYEDEPTH(o.screenPos.z);

					o.color = v.color * _TintColor;
					

					o.texcoord = TRANSFORM_TEX(v.texcoords.xy, _MainTex);
					o.texcoord2 = TRANSFORM_TEX(v.texcoords.zw, _MainTex);
					o.blend = v.texcoordBlend;

					float4 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));

					o.worldPos = worldPos;
					o.viewDir = WorldSpaceViewDir(v.vertex);

					o.uvgrab = ComputeGrabScreenPos(o.vertex);
					 o.normal.xyz = UnityObjectToWorldNormal(v.normal);

					return o;
				}

				float _InvFade;
				float _FadeRange;
				sampler2D_float _CameraDepthTexture;


				void GrabPixelValue(float2 uv, float power, inout float3 totalResult, inout float totalPower)
				{
					totalResult += UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture, uv) * power;
					totalPower+= power;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					//return float4(i.color.xyz, 1);
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); //Insert
					float2 screenUV = i.screenPos.xy / i.screenPos.w;

					half4 col = i.color * tex2D(_MainTex, i.texcoord);

					float3 viewDir = normalize(i.viewDir.xyz);

					float distToCamera = length(_WorldSpaceCameraPos - i.worldPos.xyz) - _ProjectionParams.y;

					float toCamera = smoothstep(0, 1, distToCamera);

					//float volumetriEdge = smoothstep(0, 1, 1 - col.r) * (0.1 + distToCamera * 0.1);

					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
					float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
					float fade = smoothstep(0, _InvFade, (sceneZ - i.screenPos.z)) * smoothstep(0.1, _FadeRange, length(i.worldPos - _WorldSpaceCameraPos.xyz));

					    float dott = dot(viewDir, i.normal.xyz);

					//  fade *= smoothstep(0.2, 0.5, abs(dott));

					  

					col.a = saturate(col.a * fade);

					float2 grabUv = i.uvgrab.xy / i.uvgrab.w;// + viewDir.xy * col.a;
					float2 pix = (_ScreenParams.zw - 1) * (1 + col.a*2);// *proximity;// *circle;

					//grabUv += pix * depth * 5 * _SinTime.w;

					float totalPower = 0;
					float3 sum = 0;
				   #define GRABPIXEL(dx, dy, power) GrabPixelValue(grabUv + pix * float2(dx,dy) ,power,  sum, totalPower);
				   
				 
				   //tex2Dlod( _MainTex, float4(uv + float2(kernel*xker, 0)  ,0,0)) * weight
				       GRABPIXEL(0, 0 ,159)
				GRABPIXEL(-1,  0 , 97)
                GRABPIXEL( 1,  0 , 97)
                GRABPIXEL( 0,  1 , 97)
                GRABPIXEL( 0, -1, 97)

                GRABPIXEL(-1,  1, 59)
                GRABPIXEL( 1, -1, 59)
                GRABPIXEL( 1,  1, 59)
                GRABPIXEL(-1, -1, 59)
                
                GRABPIXEL(-2,  0, 22)
                GRABPIXEL( 0, -2, 22)
                GRABPIXEL( 2,  0, 22)
                GRABPIXEL( 0,  2, 22)

                GRABPIXEL(-2,  1, 13)
                GRABPIXEL( 1, -2, 13)
                GRABPIXEL( 2,  1, 13)
                GRABPIXEL( 1,  2, 13)

                GRABPIXEL(-2,  -1, 13)
                GRABPIXEL( -1, -2, 13)
                GRABPIXEL( 2,  -1, 13)
                GRABPIXEL( -1,  2, 13)

                GRABPIXEL(-2, -2, 3)
                GRABPIXEL( 2,  2, 3)
                GRABPIXEL( 2, -2, 3)
                GRABPIXEL(-2,  2, 3)

                GRABPIXEL( 0, -3, 2)
                GRABPIXEL( 0,  3, 2)
                GRABPIXEL(-3,  0, 2)
                GRABPIXEL( 3,  0, 2)

                GRABPIXEL( 1, -3, 1)
                GRABPIXEL( 1,  3, 1)
                GRABPIXEL(-3,  1, 1)
                GRABPIXEL( 3,  1, 1)

                GRABPIXEL(-1, -3, 1)
                GRABPIXEL(-1,  3, 1)
                GRABPIXEL(-3, -1, 1)
                GRABPIXEL( 3, -1, 1)


				  sum/= totalPower;

				//	col += sum * (1-col.a) * smoothstep(0, 0.5, col.a); 
					//LerpTransparent(colA, colB, i.blend);

				//	col.a = smoothstep(0, 0.5, col.a);

					float4 result = float4(sum, smoothstep(0,0.1, col.a));

					result.rgb += col.rgb*col.a;

					return result;
				}
				ENDCG
			}
		}
	}
}
