Shader "Quiz cAnners/UI/Blick"{
	Properties
	{
		[PerRendererData] _MainTex("Mask Texture", 2D) = "white" {}

	//	_BumpMap("Bump Map", 2D) = "bump" {}

		[HDR] _Color("Tint", Color) = (1,1,1,1)

		[KeywordEnum(ScreenSpace, UvSpace)]	SPACE ("Coordinates", Float) = 0

		_Length("Length", Range(0.01,5)) = 1
		_Delay("Delay", Range(0,1)) = 0
		_Speed("Speed", Range(0.1,10)) = 1
		_Thickness("Thickness", Range(0.01,1)) = 0.75
		_Angle("Angle", Range(0,6.3)) = 6
		_SoftGlow("Soft Glow", Range(0,1)) = 0.5

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

			#pragma shader_feature_local  ___ SPACE_UVSPACE

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
				float4 screenPos : 	TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _BumpMap;
			sampler2D _Global_Noise_Lookup;
			float4 _Color;
			float4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			float _Delay;
			float4 _Effect_Time;
			float _Speed;
			float _Thickness;
			float _Angle;
			float _Length;
			float _SoftGlow;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.vertex = UnityObjectToClipPos(v.vertex);
				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				OUT.screenPos = ComputeScreenPos(OUT.vertex);
				OUT.color = v.color * _Color;

				return OUT;
			}

			float4 frag(v2f IN) : SV_Target
			{
				float2 blickUv;

				#if SPACE_UVSPACE
					blickUv = IN.texcoord;
				#else
					blickUv = IN.screenPos.xy / IN.screenPos.w;
				#endif

				//float4 bump = tex2D(_BumpMap, IN.texcoord);

				//blickUv += (bump.xy - 0.5) * 0.2;

				// Rotate
				float si = sin(_Angle);
				float co = cos(_Angle);
				float blickPos = co * blickUv.x - si * blickUv.y;

				blickPos += _Effect_Time.y  * _Speed + _Delay * _Length + 50;

				float blickSegment = (blickPos % _Length) / _Length;

				blickSegment = 1 - abs(blickSegment - 0.5) * 2;
				
				float cutoff = fwidth(blickSegment) * 2;

				float softGlow = saturate(0.01  / (max((blickSegment - _Thickness) , 0.00001) + 0.01))
				* (1.1 - 0.2 * tex2D(_Global_Noise_Lookup, blickUv*10 +_Effect_Time.y ));
				; // smoothstep(1 , _Thickness, blickSegment);

				//return softGlow;

				float sharp = smoothstep(_Thickness, _Thickness - (cutoff + 0.00001), blickSegment);

				float textureAlpha = tex2D(_MainTex, IN.texcoord).a;

				IN.color.a *= (sharp + softGlow * _SoftGlow * (1-sharp)) * textureAlpha;

				return IN.color;

			}
			ENDCG
		}
	}

  Fallback "Legacy Shaders/Transparent/VertexLit"
}