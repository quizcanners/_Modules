Shader "Quiz cAnners/UI/Images/Motion Blur"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}

      //  _Force("Blur Force", Range(0,1)) = 0
      //  _Angle("Angle", Range(0,6.3)) = 6

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
    //    ZTest[unity_GUIZTestMode]
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float2 strengthAndAngle : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
         //   float _Force;
         //   float _Angle;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex) - 0.5;

                OUT.strengthAndAngle = v.texcoord1.xy;

                OUT.texcoord *= 2 +  OUT.strengthAndAngle.x * 0.2;

                OUT.texcoord += 0.5;

                OUT.screenPos = ComputeScreenPos(OUT.vertex); 

                OUT.color = v.color;
                return OUT;
            }

            float2 Rot(float2 uv, float angle) 
            {
		        float si = sin(angle);
		        float co = cos(angle);
		        return float2(co * uv.x - si * uv.y, si * uv.x + co * uv.y);
	        }


            float4 frag(v2f IN) : SV_Target
            {
                float2 uv =  IN.texcoord;

                float2 dx = ddx(uv);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w; 

                // Mip Level
                float2 px = _MainTex_TexelSize.z * dx;
	            float2 py = _MainTex_TexelSize.w * ddy(uv);

                float2 rotation = normalize(dx);

                float sizeOnScreen = length(fwidth(uv)); 

                float _Force = IN.strengthAndAngle.x;
                float _Angle = IN.strengthAndAngle.y;

	            float mipLevel = (max(0, 0.25 * log2(max(dot(px, px), dot(py, py))))) + _Force;

                float2 blurVector = Rot(rotation, -_Angle) * smoothstep(0, 1, sizeOnScreen * 20 * _Force) * 0.15 ; //_MainTex_TexelSize.x;


                float4 color0 = tex2Dlod(_MainTex, float4(uv - blurVector * 4 , 0, mipLevel));
                float4 color1 = tex2Dlod(_MainTex, float4(uv - blurVector * 3 , 0, mipLevel));
                float4 color2 = tex2Dlod(_MainTex, float4(uv - blurVector * 2 , 0, mipLevel));
                float4 color3 = tex2Dlod(_MainTex, float4(uv - blurVector     , 0, mipLevel));
                float4 color4 = tex2Dlod(_MainTex, float4(uv ,                  0, mipLevel));
                float4 color5 = tex2Dlod(_MainTex, float4(uv + blurVector     , 0, mipLevel));
                float4 color6 = tex2Dlod(_MainTex, float4(uv + blurVector * 2 , 0, mipLevel));
                float4 color7 = tex2Dlod(_MainTex, float4(uv + blurVector * 3 , 0, mipLevel));
                float4 color8 = tex2Dlod(_MainTex, float4(uv + blurVector * 4 , 0, mipLevel));

                float4 col;
                col.rgb =   color0.rgb * color0.a +
                            color1.rgb * color1.a+
                            color2.rgb * color2.a+
                            color3.rgb * color3.a+
                            color4.rgb * color4.a+
                            color5.rgb * color5.a+
                            color6.rgb * color6.a+
                            color7.rgb * color7.a+
                            color8.rgb * color8.a;

				col.a = color0.a + color1.a + color2.a + color3.a + color4.a + color5.a + color6.a  + color7.a  + color8.a;

                col/=9;

				col.rgb /= col.a + 0.001;


                return col * IN.color;
            }
        ENDCG
        }
    }
}