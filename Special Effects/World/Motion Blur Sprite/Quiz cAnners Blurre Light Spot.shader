Shader "Quiz cAnners/Effects/Blurred Light Spot" 
{
	Properties
	{
		_MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_Force("Blur Force", Range(0,1)) = 0
        _Angle("Angle", Range(0,6.3)) = 6
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

				sampler2D _MainTex;
				sampler2D _CameraDepthTexture;
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
				};

				v2f vert(appdata_full v) 
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					
					o.screenPos = ComputeScreenPos(o.pos); 

					o.texcoordOriginal =  v.texcoord.xy;

					o.texcoord = v.texcoord.xy - 0.5;
					o.texcoord *= 3 +  _Force * 0.2;
					//o.texcoord += 0.5;

					return o;
				}

				float2 Rot(float2 uv, float angle) 
				{
					float si = sin(angle);
					float co = cos(angle);
					return float2(co * uv.x - si * uv.y, si * uv.x + co * uv.y);
				}

				inline float DistToLine(float2 pos, float2 a, float2 b) 
				{
					float2 pa = pos - a;
					float2 ba = b - a;
					float t = saturate(dot(pa, ba)/dot(ba,ba));
					return length(pa - ba * t);
				}

				float4 frag(v2f i) : COLOR
				{
					float2 uv =  i.texcoord;
					float2 dx = ddx(uv);
					float2 screenUV = i.screenPos.xy / i.screenPos.w; 

					// Mip Level
					float2 px = _MainTex_TexelSize.z * dx;
					float2 py = _MainTex_TexelSize.w * ddy(uv);

					float2 rotation = normalize(dx);
					float sizeOnScreen = length(fwidth(uv)); 

					float2 blurVector =Rot(rotation, -_Angle) * sizeOnScreen *  15 * _Force ; //_MainTex_TexelSize.x;

					float dist = DistToLine(uv, 0, blurVector);//length(uv);

					float diff = length(fwidth(uv));

					float alpha = diff/(dist + 0.001);
				
					alpha *= smoothstep(0.5, 0.3, length(i.texcoordOriginal-0.5));


					float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
					float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(depth));
					float partZ = i.screenPos.z;
					float fade = smoothstep(0, 1, 0.2 * (sceneZ - partZ));

					alpha *= fade;

					float4 col = _Color * alpha / (1 + length(blurVector));

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

