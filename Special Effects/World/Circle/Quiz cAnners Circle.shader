﻿Shader "Quiz cAnners/Effects/Circle_Additive" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_Hardness("Hardness", Range(1,16)) = 2
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

				float4 _Color;
				float _Hardness;

				struct v2f {
					float4 pos : SV_POSITION;
					float2 texcoord : TEXCOORD2;

				};

				v2f vert(appdata_full v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.texcoord = v.texcoord.xy;
					return o;
				}

				float4 frag(v2f i) : COLOR{

					float2 off = i.texcoord - 0.5;
					off *= off;
					float len = (off.x + off.y) * 2;

					float diff = length(fwidth(i.texcoord));

					float alpha = 1- smoothstep(0.5 - diff * 4, 0.5, len); 

					_Color.a *= alpha;

					return _Color;
				}
				ENDCG

			}
		}
		Fallback "Legacy Shaders/Transparent/VertexLit"
	}
}

