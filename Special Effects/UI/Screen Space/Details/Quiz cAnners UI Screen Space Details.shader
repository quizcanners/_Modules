﻿Shader "Quiz cAnners/UI/Effects/Screen Space Details" {
	Properties
	{
		[PerRendererData]
		_MainTex("Sprite Texture", 2D) = "white" {}
		_Details("Details Texture", 2D) = "gray" {}
		_OverrideMask("Override Mask", 2D) = "white" {}
	
		_Brightness("Brightness", Range(-1,1)) = 0.5
		_Visibility("Visibility", Range(0,1)) = 0.2
		_Override("Override", Range(0,1)) = 0.2

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
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
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask RGB
			//ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				half4 color			: COLOR;
				float4 screenPos	: TEXCOORD0;
				float4 worldPosition: TEXCOORD1;
				float2 texcoord		: TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			uniform float _Brightness;
			uniform float _Visibility;
			uniform sampler2D _MainTex;

			uniform float4 _ClipRect;
			uniform float4 _MainTex_TexelSize;
			uniform float4 _Details_TexelSize;
			uniform float _Override;

			v2f vert(appdata_full v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.texcoord = v.texcoord;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;

				return o;
			}

			uniform sampler2D _Details;
			uniform float4 _Details_ST;
			sampler2D _OverrideMask;
			uniform float4 _OverrideMask_ST;

			float4 frag(v2f o) : SV_Target {

				o.screenPos.xy = o.screenPos.xy / o.screenPos.w * _ScreenParams.xy * _Details_TexelSize.xy;

				float2 screenUv = o.screenPos.xy * _Details_ST.xy;

				float4 details = tex2Dlod(_Details, float4(o.screenPos.xy * _Details_ST.xy ,0,0)) ;

				float4 overrideMask = tex2Dlod(_OverrideMask, float4(o.screenPos.xy * _OverrideMask_ST.xy ,0,0)) ;

				float4 col = tex2D(_MainTex, o.texcoord.xy)* o.color;

				col.rgb *= lerp(1, details.rgb * (1+_Brightness), _Visibility);

				col.rgb = lerp(col.rgb, details.rgb, _Override * overrideMask);

				#ifdef UNITY_UI_CLIP_RECT
				col.a *= UnityGet2DClipping(o.worldPosition.xy, _ClipRect);
				#endif

				return col;
			}
			ENDCG
		}
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}