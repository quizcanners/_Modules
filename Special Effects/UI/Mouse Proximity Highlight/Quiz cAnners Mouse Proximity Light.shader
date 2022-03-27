Shader "Quiz cAnners/UI/Mouse Proximity Light"
{
    Properties
    {
        [PerRendererData]
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        [Toggle(_MASK_IS_RED)] reIsAlpha("Use Mask Red as Alpha", Float) = 0
        [Toggle(_HIGHLIGHT_ON_PRESS)] whenPressed("Highlight when pressed", Float) = 0
        [Toggle(_DEBUG)] debugLine("Debug", Float) = 0

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
        ZTest Off
        Blend SrcAlpha One //MinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#           pragma shader_feature_local ___ _MASK_IS_RED
#           pragma shader_feature_local ___ _DEBUG
#           pragma shader_feature_local ___ _HIGHLIGHT_ON_PRESS
            
           
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                half4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : 	TEXCOORD2;

                float2 stretch		: TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Effect_Time;
            sampler2D _qcPp_Global_Screen_Effect;
            fixed4 _Color;
            float4 _qcPp_MousePosition;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                OUT.color = v.color * _Color;

                float screenAspect = _ScreenParams.x * (_ScreenParams.w - 1);
                float2 aspectCorrection = float2(1, 1);
                if (screenAspect > 1)
                    aspectCorrection.y = (1 / screenAspect);
                else
                    aspectCorrection.x = (screenAspect);
                OUT.stretch = aspectCorrection;

                return OUT;
            }


            fixed4 frag(v2f IN) : SV_Target {

                float2 screenPos = IN.screenPos.xy / IN.screenPos.w;
                float2 bumpUv;

                bumpUv = ((screenPos - 0.5) * IN.stretch.xy + 0.5) * 4.25; 
               
                float alpha = tex2D(_MainTex, IN.texcoord)
#                   if _MASK_IS_RED
                        .r
#                   else
                        .a
#                   endif
                    * IN.color.a;
            

#                   if _DEBUG
                IN.color.a *= alpha;
                return IN.color;
#                   endif

                half2 fromMouse = (screenPos - _qcPp_MousePosition.xy);
                fromMouse.x *= _qcPp_MousePosition.w;
                float lenM = length(fromMouse);// *4;

                IN.color.a *= alpha * smoothstep(0.2, 0, lenM + sin(lenM * 50 - _Effect_Time * 50) * 0.02) 
#               if _HIGHLIGHT_ON_PRESS
                    * _qcPp_MousePosition.z
#               endif
                    ;


                return  IN.color;
            }
        ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/VertexLit"
}