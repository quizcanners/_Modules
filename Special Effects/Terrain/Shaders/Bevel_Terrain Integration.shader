﻿Shader "Playtime Painter/Terrain Integration/BevelGeometry" {
	Properties{
		[NoScaleOffset]_MainTex_ATL("Base texture (_ATL)", 2D) = "white" {}
		[KeywordEnum(None, Regular, Combined)] _BUMP("Bump Map", Float) = 0
		[NoScaleOffset]_BumpMapC_ATL("Combined Maps (_ATL) (RGB)", 2D) = "white" {}
		[Toggle(_qcPp_UV_PROJECTED)] _PROJECTED("Projected UV", Float) = 0
		_Merge("_Merge", Range(0.01,2)) = 1
		[Toggle(_qcPp_UV_ATLASED)] _ATLASED("Is Atlased", Float) = 0
		[NoScaleOffset]_qcPp_AtlasTextures("_Textures In Row _ Atlas", float) = 1
	}

	SubShader{

		Tags{
			"RenderType" = "Opaque"
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
			"DisableBatching" = "True"
			"Solution" = "Bevel"
		}

		ColorMask RGBA

		Pass{

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Assets\The-Fire-Below\Common\Shaders\qc_terrain_cg.cginc"

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma shader_feature_local  ___ _qcPp_UV_ATLASED
			#pragma shader_feature_local  ___ _qcPp_UV_PROJECTED
			#pragma shader_feature_local  ___ _BUMP_NONE _BUMP_REGULAR _BUMP_COMBINED 

			sampler2D _MainTex_ATL;
			sampler2D _BumpMapC_ATL;
			float4 _MainTex_ATL_TexelSize;
			float _qcPp_AtlasTextures;

			struct v2f {
				float4 pos : SV_POSITION;
				float4 vcol : COLOR0;
				float3 wpos : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float2 texcoord : TEXCOORD2;
				float4 edge : TEXCOORD3;
				float3 snormal: TEXCOORD4;
				SHADOW_COORDS(5)
				float3 viewDir: TEXCOORD6;
				float3 edgeNorm0 : TEXCOORD7;
				float3 edgeNorm1 : TEXCOORD8;
				float3 edgeNorm2 : TEXCOORD9;
				
				#if !_BUMP_NONE
				#if _qcPp_UV_PROJECTED
				float4 bC : TEXCOORD10;
				#else
				float4 wTangent : TEXCOORD10;
				#endif
				#endif
				UNITY_FOG_COORDS(11)
				float3 tc_Control : TEXCOORD12;
				#if _qcPp_UV_ATLASED
				float4 atlasedUV : TEXCOORD13;
				#endif
			};

			v2f vert(appdata_full v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				UNITY_TRANSFER_FOG(o, o.pos);
				o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
				#if _qcPp_WATER_FOAM
				o.fwpos = ComputeFoam(o.wpos);
				#endif
				o.tc_Control.xyz = (o.wpos.xyz - _qcPp_mergeTeraPosition.xyz) / _qcPp_mergeTerrainScale.xyz;
				o.normal.xyz = UnityObjectToWorldNormal(v.normal);

				o.vcol = v.color;
				o.edge = float4(v.texcoord1.w, v.texcoord2.w, v.texcoord3.w, 1);
				o.viewDir.xyz = WorldSpaceViewDir(v.vertex);

				float3 deEdge = 1 - o.edge.xyz;

				o.edgeNorm0 = UnityObjectToWorldNormal(v.texcoord1.xyz);
				o.edgeNorm1 = UnityObjectToWorldNormal(v.texcoord2.xyz);
				o.edgeNorm2 = UnityObjectToWorldNormal(v.texcoord3.xyz);

				o.snormal.xyz = normalize(o.edgeNorm0*deEdge.x + o.edgeNorm1*deEdge.y + o.edgeNorm2*deEdge.z);

				#if _qcPp_UV_PROJECTED
				normalAndPositionToUV(o.snormal.xyz, o.wpos,
				#if !_BUMP_NONE
					o.bC,
				#endif
					o.texcoord.xy);

				#else

				#if !_BUMP_NONE
				o.wTangent.xyz = UnityObjectToWorldDir(v.tangent.xyz);
				o.wTangent.w = v.tangent.w * unity_WorldTransformParams.w;
				#endif

				o.texcoord = v.texcoord.xy;

				#endif

				TRANSFER_SHADOW(o);

				#if _qcPp_UV_ATLASED
				vert_atlasedTexture(_qcPp_AtlasTextures, v.texcoord.z, o.atlasedUV);
				#endif

				return o;
			}

			float4 frag(v2f i) : SV_Target{

				i.viewDir.xyz = normalize(i.viewDir.xyz);

				float caustics = 0;

				#if _qcPp_WATER_FOAM
				float underWater = max(0, _qcPp_foamParams.z - i.wpos.y);
				float3 projectedWpos;
			
				float3 nrmNdSm = SAMPLE_WATER_NORMAL(i.viewDir.xyz,   projectedWpos, i.tc_Control, caustics, underWater);

				underWater = min(1, underWater);

				caustics *= underWater;

				#endif

				float dist = length(i.wpos.xyz - _WorldSpaceCameraPos.xyz)+1;

				float mip = 0;

				#if _qcPp_UV_ATLASED
				atlasUVlod(i.texcoord.xy, mip, _MainTex_ATL_TexelSize, i.atlasedUV);
				#endif

				#if _qcPp_UV_ATLASED
				float4 col = tex2Dlod(_MainTex_ATL, float4(i.texcoord,0,mip));
				#else
				float4 col = tex2D(_MainTex_ATL, i.texcoord);
				#endif

				float weight;
				float3 worldNormal = DetectSmoothEdge(
						i.edge, i.normal.xyz, i.snormal.xyz, i.edgeNorm0, i.edgeNorm1, i.edgeNorm2, weight);

				float deWeight = 1 - weight;

				//return float4(worldNormal, 1);

				//clip(dot(i.viewDir.xyz, worldNormal));

				col = col*deWeight + i.vcol*weight;

				#if !_BUMP_NONE
				#if _qcPp_UV_ATLASED  
					float4 bumpMap = tex2Dlod(_BumpMapC_ATL, float4(i.texcoord, 0, mip));
				#else
					float4 bumpMap = tex2D(_BumpMapC_ATL, i.texcoord);
				#endif

				float3 tnormal;

				#if _BUMP_REGULAR
				tnormal = UnpackNormal(bumpMap);
				bumpMap = float4(0,0,1,1);
				#else
				bumpMap.rg = (bumpMap.rg - 0.5) * 2;
				tnormal = float3(bumpMap.r, bumpMap.g, 1);
				#endif

				float3 preNorm = worldNormal;

				#if _qcPp_UV_PROJECTED
				applyTangentNonNormalized(i.bC, worldNormal, bumpMap.rg);
				worldNormal = normalize(worldNormal);
				#else
				ApplyTangent(worldNormal, tnormal,  i.wTangent);
				#endif

				worldNormal = worldNormal*deWeight + preNorm*weight;

				#else
				float4 bumpMap = float4(0,0,1,1); // MODIFIED
				#endif

				col.a = col.a*deWeight + weight*i.vcol.a;
				bumpMap.a = bumpMap.a*deWeight + weight*0.7;

				// Terrain Start
				float4 terrainN = 0;

				Terrain_Trilanear(i.tc_Control, i.wpos, dist, worldNormal, col, terrainN, bumpMap);
	
				float shadow = SHADOW_ATTENUATION(i);

				float Metalic = 0;

				float ambient = terrainN.a;
				float smoothness = col.a;


#if _qcPp_WATER_FOAM
				col.rgb += 

				APPLY_PROJECTED_WATER(saturate(underWater), worldNormal, nrmNdSm, i.tc_Control, projectedWpos, i.viewDir.y, col, smoothness, ambient, shadow, caustics);
#endif


				Terrain_Water_AndLight(col,  i.tc_Control, ambient, smoothness, worldNormal, i.viewDir.xyz,  shadow);

				UNITY_APPLY_FOG(i.fogCoord, col);

				BleedAndBrightness(col, 1, i.texcoord.xy*100);

				return col;

			}
			ENDCG

		}

		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}
	FallBack "Diffuse"
}