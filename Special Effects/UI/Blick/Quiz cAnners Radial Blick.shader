Shader "Quiz cAnners/UI/Blick Radial"{
	Properties
	{
		[PerRendererData] _MainTex("Mask Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		_Speed("Speed", Range(0,5)) = 1
		_Thickness("Thickness", Range(0.001,1)) = 0.25
		_ArcSize("Arc Size", Range(0.23,0.49)) = 0.25

		[Toggle(_SHARP_EDGES)] sharpEdges("Sharp edges of the segment", Float) = 0

	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha One 
		ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma shader_feature_local  ___  _SHARP_EDGES

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _Color;
			float4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			float4 _Effect_Time;
			float _Speed;
			float _ArcSize;
			float _Thickness;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				OUT.color = v.color * _Color;

				return OUT;
			}

			half4 frag(v2f IN) : SV_Target{

				float2 uv = IN.texcoord - 0.5;
				float w = fwidth(IN.texcoord.xy) ;
				const float PI2 = 3.14159265359 * 2;
				float pixel_angle = atan2(uv.x, uv.y) / PI2 + 0.5;
				float pixel_distance = length(uv) * 2;

				float2 radUv = float2(pixel_angle, pixel_distance);

				float effect_center = _Effect_Time.y * _Speed + 1;

				float mirror = (effect_center - pixel_angle);

				float pixel_to_center = max(abs((mirror) % 1 - 0.5), abs((mirror + 0.5) % 1 - 0.5));

				float arc = smoothstep(_ArcSize,  
#if _SHARP_EDGES
					_ArcSize + w
#else
					0.5
#endif
					, pixel_to_center);


				float circle = (smoothstep(_Thickness , _Thickness - w * 5, 1-pixel_distance)) * smoothstep(1, 1-w * 5, pixel_distance);

				IN.color *= circle * arc;

				float4 mask = tex2D(_MainTex, IN.texcoord);

				return  IN.color * mask;
			}
			ENDCG
		}
	}

  Fallback "Legacy Shaders/Transparent/VertexLit"
}