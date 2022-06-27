Shader "Quiz cAnners/Effects/Blurred Sprite" 
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
		 Blend SrcAlpha OneMinusSrcAlpha

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
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;
				float4 _Color;
				float _Force;
				float _Angle;

				struct v2f 
				{
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					float4 screenPos : TEXCOORD1;
				};

				v2f vert(appdata_full v) 
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					
					o.screenPos = ComputeScreenPos(o.pos); 

					o.texcoord = v.texcoord.xy - 0.5;
					o.texcoord *= 3 +  _Force * 0.2;
					o.texcoord += 0.5;

					return o;
				}

				float2 Rot(float2 uv, float angle) 
				{
					float si = sin(angle);
					float co = cos(angle);
					return float2(co * uv.x - si * uv.y, si * uv.x + co * uv.y);
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

					float mipLevel = (max(0, 0.5 * log2(max(dot(px, px), dot(py, py))))) + _Force;

					float2 blurVector =Rot(rotation, -_Angle) * sizeOnScreen *  4 * _Force ; //_MainTex_TexelSize.x;

					float4 color0 = tex2Dlod(_MainTex, float4(uv - blurVector, 0, mipLevel));
					float4 color1 = tex2Dlod(_MainTex, float4(uv  , 0, mipLevel));
					float4 color2 = tex2Dlod(_MainTex, float4(uv + blurVector , 0, mipLevel));
					float4 color3 = tex2Dlod(_MainTex, float4(uv + blurVector *2 , 0, mipLevel));
					float4 color4 = tex2Dlod(_MainTex, float4(uv + blurVector *3 , 0, mipLevel));

					float4 col;
					col.rgb =   color0.rgb * color0.a +
								color1.rgb * color1.a+
								color2.rgb * color2.a+
								color3.rgb * color3.a+
								color4.rgb * color4.a;

					col.a = color0.a + color1.a + color2.a + color3.a + color4.a;
					col/=5;
					col.rgb /= col.a + 0.001;

					return col * _Color;
				}
				ENDCG
			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

