﻿Shader "Quiz cAnners/UI/Effects/ScreenSpacePixelPerfect" {
	Properties
	{
		[PerRendererData]
		_MainTex("Sprite Texture", 2D) = "white" {}
	
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
		ColorMask[_ColorMask]

		Pass
		{
			Name "Default"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				half4 color			: COLOR;
				float4 screenPos	: TEXCOORD0;
				float4 worldPosition: TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform sampler2D _MainTex;
			uniform float4 _ClipRect;
			uniform float4 _MainTex_TexelSize;

			v2f vert(appdata_full v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;

				return o;
			}

			float4 frag(v2f o) : SV_Target {

				o.screenPos.xy = o.screenPos.xy / o.screenPos.w * _ScreenParams.xy * _MainTex_TexelSize.xy;

				float4 color = tex2Dlod(_MainTex, float4(o.screenPos.xy ,0,0)) * o.color;

				return color;
			}
			ENDCG
		}
	}

	Fallback "Legacy Shaders/Transparent/VertexLit"
}