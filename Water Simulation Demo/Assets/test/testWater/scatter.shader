// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "testShaders/scatter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	    _WaterScatteringKd ("Water Scattering Kd", Float) = 1
	    _WaterScatteringAttenuation ("Water Scattering Attenuation", Float) = 1
	    _WaterScatteringDiffuseRadiance ("Water Scattering Diffuse Radiance", Float) = 1

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

	        struct appdata {
	           float4 vertex : POSITION;
	           float2 uv : TEXCOORD0;
	        };

	        struct v2f {
	           float3 screenPos : TEXCOORD0;
	           float2 uv : TEXCOORD1;
	           float3 outScattering : TEXCOORD2;
	           float3 inScattering : TEXCOORD3;
	           float4 vertex : SV_POSITION;
	        };

			sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _UnScatteringRefrTex;
            float3 _WaterScatteringKd;
            float3 _WaterScatteringAttenuation;
            float3 _WaterScatteringDiffuseRadiance;
            float _WaterAltitude;
                           
            void SimpleWaterScattering(half viewDist, half3 worldPos, half depth, half3 diffuseRadiance,
                  half3 atten, half3 kd, out half3 outScattering, out half3 inScattering) { 
                  
                  float d = (viewDist*depth) / (_WorldSpaceCameraPos.y - worldPos.y);
                  outScattering= exp(-atten*d);
                  inScattering= diffuseRadiance* (1 - outScattering*exp(-depth*kd));
            }
            
            v2f vert(appdata v) {
	            v2f o;
  	            o.vertex = UnityObjectToClipPos(v.vertex);
	            o.uv = TRANSFORM_TEX(v.uv, _MainTex);
	            o.screenPos = ComputeScreenPos(o.vertex).xyw;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 outScattering = float3(1,0,0);
                float3 inScattering = float3(1,0,0);
                float3 viewVector = worldPos - _WorldSpaceCameraPos.xyz;
                float depth = max(0, _WaterAltitude - worldPos.y);
 
                SimpleWaterScattering(length(viewVector),worldPos.xyz,depth,_WaterScatteringDiffuseRadiance,
            	_WaterScatteringAttenuation, _WaterScatteringKd, outScattering, inScattering);

                o.outScattering = outScattering;
                o.inScattering = inScattering;
  
                return o;
            }
                            
            half4 frag (v2f i) : SV_Target {
                  //sample the texture
                  half4 result = half4(0,0,0,1);
                  float2 srcUV = i.screenPos.xy/i.screenPos.z;                                    
                  half4 refractionColor = tex2D(_UnScatteringRefrTex, srcUV); 
                  result.xyz = refractionColor.xyz * i.outScattering + i.inScattering;
                  result.w = refractionColor.w;                  
 
                  return result; 
            }
			ENDCG
		}
	}
}
