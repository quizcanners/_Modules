Shader "Quiz cAnners/Effects/Unstrtched Circles" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		[Toggle(_DEBUG)] debugOn("Debug", Float) = 0
		[Toggle(_BILLBOARD)] billboardValue("Bill Board", Float) = 0
	}

		SubShader{

			Tags{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
			}

			ColorMask RGB
			Cull Off
			ZWrite Off
			ZTest Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass{

				CGPROGRAM

				#include "UnityCG.cginc"

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				#pragma multi_compile_fwdbase // useful to have shadows 
				#pragma shader_feature_local ____ _DEBUG 
				#pragma shader_feature_local ___ _BILLBOARD

				#pragma target 3.0

				struct v2f {
					float4 pos : 		SV_POSITION;
					float3 worldPos : 	TEXCOORD0;
					float3 normal : 	TEXCOORD1;
					float2 texcoord : 	TEXCOORD2;
					float3 viewDir: 	TEXCOORD3;
					float4 screenPos : 	TEXCOORD4;
					float4 color: 		COLOR;
				};


				uniform float4 _MainTex_ST;
				sampler2D _MainTex;

				v2f vert(appdata_full v) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);

#if _BILLBOARD
					// billboard mesh towards camera
					float3 vpos = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
					float4 worldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
					float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
					o.pos = mul(UNITY_MATRIX_P, viewPos);
#else
					o.pos = UnityObjectToClipPos(v.vertex);
#endif

					o.normal.xyz = UnityObjectToWorldNormal(v.normal);
					
					o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.viewDir.xyz = WorldSpaceViewDir(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					o.screenPos = ComputeScreenPos(o.pos);
					o.color = v.color;
					return o;
				}

				float4 frag(v2f o) : COLOR{

					// Usually you'll see 'v2f i' for Input, but this saves time when I copy stuff between 
					// vert and frag functions.


					const float downscale = 5;

					float dx = ddx(o.texcoord.x * downscale);
					float dy = ddy(o.texcoord.y * downscale);

				//	float diff = abs( fwidth(o.texcoord.xy));

					o.texcoord.xy = o.texcoord.xy % 1;
				
					float2 off = abs(o.texcoord.xy - 0.5);

					
				//	off.x *= diffY * 25;
					//off.y *= diffX * 25 ;

					off.x = off.x / (dx ) ;
					off.y = off.y / (dy );


					off *= off;

				

					//off.x = smoothstep(0.25 - diffX * 4, 0.25, abs(off.x));
					//off.y = smoothstep(0.25 - diffY * 4, 0.25, abs(off.y));

					//float rel = diffX / diffY;

					//off *= float2(rel, 1/rel);

					//return rel;
					//off.x = smoothstep(0, 1-diffX*10, off.x);

					//float rel = diffX / diffY;


					//off *= float2(rel, 1/rel);
					//off *= off;

					//o.viewDir.xyz = normalize(o.viewDir.xyz);
					//float2 duv = o.screenPos.xy / o.screenPos.w;

					float4 col = o.color * tex2D(_MainTex, o.texcoord.xy);

					col.a *= saturate(1 - (off.x + off.y) * 4);

					#if _DEBUG 
					col.a = 1;
					#endif

					return col;
				}
				ENDCG
			}
		}
			Fallback "Legacy Shaders/Transparent/VertexLit"

					//CustomEditor "CircleDrawerGUI"
}