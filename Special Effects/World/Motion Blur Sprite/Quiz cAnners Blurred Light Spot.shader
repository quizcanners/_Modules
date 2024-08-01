Shader "Quiz cAnners/Effects/Blurred Light Spot" 
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_Force("Blur Force", Range(0,1)) = 0
        _Angle("Angle", Range(0,6.3)) = 6
		_Visibility ("Visibility", Range(0,1)) = 1
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
		ZTest Off
		Blend SrcAlpha One //MinusSrcAlpha

		SubShader
		{
			Pass
			{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma multi_compile_instancing
				#pragma target 3.0

				#pragma multi_compile ___ qc_LAYARED_FOG
			    #include "Assets/Qc_Rendering/Shaders/Savage_Sampler_Transparent.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;
				float4 _Color;
				float _Force;
				float _Angle;

				struct v2f 
				{
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float2 texcoordOriginal : TEXCOORD1;
					float4 screenPos : TEXCOORD2;
					float3 worldPos : TEXCOORD3;
				};

				v2f vert(appdata_full v) 
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					
					o.screenPos = ComputeScreenPos(o.pos); 
						COMPUTE_EYEDEPTH(o.screenPos.z);
					o.texcoordOriginal =  v.texcoord.xy;

					o.texcoord = v.texcoord.xy - 0.5;
					o.texcoord *= 3 +  _Force * 0.2;
					//o.texcoord += 0.5;
						o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					return o;
				}

				float2 Rot(float2 uv, float angle) 
				{
					float si = sin(angle);
					float co = cos(angle);
					return float2(co * uv.x - si * uv.y, si * uv.x + co * uv.y);
				}

				/*
				inline float DistToLine(float2 pos, float2 a, float2 b) 
				{
					float2 pa = pos - a;
					float2 ba = b - a;
					float t = saturate(dot(pa, ba)/dot(ba,ba));
					return length(pa - ba * t);
				}*/

				inline float DistToLine(float2 pos, float2 b)
				{
					float t = saturate(dot(pos, b) / dot(b, b));
					return length(pos - b * t);
				}


				float _Visibility;

				float4 frag(v2f i) : COLOR
				{
					float2 uv =  i.texcoord;
					float2 dx = ddx(uv);
					float2 screenUV = i.screenPos.xy / i.screenPos.w; 

					// Mip Level
					//float2 px = _MainTex_TexelSize.z * dx;
					//float2 py = _MainTex_TexelSize.w * ddy(uv);

					float2 rotation = normalize(dx);
					float sizeOnScreen = length(fwidth(uv)); 

					float2 blurVector =Rot(rotation, -_Angle) * sizeOnScreen *  15 * _Force ; //_MainTex_TexelSize.x;

					float dist = DistToLine(uv, blurVector);//length(uv);

				//	float diff = length(fwidth(uv));

						float alpha = sizeOnScreen;

						float distToCam = length(_WorldSpaceCameraPos - i.worldPos.xyz);

					#if qc_LAYARED_FOG
						float4 foggy = SampleLayeredFog(distToCam, screenUV);

						alpha /= (dist * 2 + 0.001);
					#else
						alpha /= (dist * 2  + 0.001);
					#endif

					//return alpha;
				
					float offUVLen = length(i.texcoordOriginal-0.5);
				
					alpha *= smoothstep(0.5, 0.3, offUVLen);


					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
					float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
					float partZ = i.screenPos.z;
					float fade = smoothstep(0, 1, (sceneZ - partZ));

					float contact = smoothstep(distToCam * 0.03, 0, abs(sceneZ - partZ));

					float contactLight = contact * smoothstep(0.5,0,offUVLen);

					alpha *= fade;

					float4 col = _Color * alpha * 10 / (10 + length(blurVector));
					col.a = min(_Visibility, col.a);

					col += _Color * _Visibility * contactLight;



					#if qc_LAYARED_FOG

						col.rgb = lerp(col.rgb, col.rgb*foggy.rgb, foggy.a);

					#else 
					
						float3 mix = col.gbr + col.brg;
						col.rgb += mix * mix * 0.05;

					#endif

					//ApplyLayeredFog_Transparent(col,screenUV, i.worldPos.xyz);

					return col;

				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

