Shader "Quiz cAnners/UI/Images/ImageTransition" {
	Properties{
		[PerRendererData] _MainTex("Mask (RGB)", 2D) = "white" {}
		[NoScaleOffset]_MainTex_Current("First Texture", 2D) = "black" {}
		[NoScaleOffset]_Next_MainTex("Next Texture", 2D) = "black" {}
		_Transition("Transition", Range(0,1)) = 0
		
	}

	Category
	{
		Tags
		{ 
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"LightMode" = "ForwardBase"
			"Queue" = "Transparent"
		}

		Cull Off
		LOD 200
		ColorMask RGB
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader
		{
			Pass
			{

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				sampler2D _MainTex_Current;
				float4 _MainTex_Current_TexelSize;
				sampler2D _Next_MainTex;
				float4 _MainTex_Current_ST;

				float _Transition;

				struct v2f 
				{
					float4 pos : POSITION;
					float2 texcoord : TEXCOORD2;
					float4 color : COLOR;
				};

				v2f vert(appdata_full v) 
				{
					v2f o;

					o.texcoord = v.texcoord;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					return o;
				}

			    float4 LerpTransparent(float4 col1, float4 col2, float transition)
				{
					float4 col;
                
					col.rgb = lerp(col1.rgb * col1.a, col2.rgb * col2.a, transition);
					col.a = lerp(col1.a, col2.a, transition);
					col.rgb /= col.a + 0.001;

					return col;
				}

				float4 frag(v2f i) : COLOR
				{
					float4 col = tex2D(_MainTex_Current,  i.texcoord);
					float4 col2 = tex2D(_Next_MainTex,  i.texcoord);
					col = LerpTransparent(col, col2, _Transition);
					col *= tex2D(_MainTex,  i.texcoord);
					return saturate(col);
				}
				ENDCG
			}
		}
	}
}
