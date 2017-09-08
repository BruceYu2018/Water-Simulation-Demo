Shader "demoShaders/demoWaveDisplay"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		_wavePosTex ("Wave Position", 2D) = "black" {}
		_waveNormal ("Wave Normal", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

	    CGINCLUDE

		#include "UnityCG.cginc"

	    struct appdata{
	        float4 vertex : POSITION;
			float4 tangent : TANGENT;
			float3 normal : NORMAL;
			float2 texcoord : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
			float2 texcoord2 : TEXCOORD2;
	    };

		struct Input {
			float2 uv_MainTex : TEXCOORD0;
		};
        
        float4 _Color;
		sampler2D _MainTex;
		sampler2D _wavePosTex;
		sampler2D _waveNormal;

		void vert (inout appdata v) {
			float4 vertex = tex2Dlod(_wavePosTex, float4(v.texcoord, 0, 0));
			v.vertex.y = vertex.y;
			v.normal = tex2Dlod(_waveNormal, float4(v.texcoord, 0, 0));

		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 col = tex2D(_MainTex, IN.uv_MainTex)*_Color;

		    o.Albedo = col.rgb;
			o.Alpha = col.a;
		}
	
		ENDCG

		CGPROGRAM
		      #pragma surface surf Lambert addshadow vertex:vert
		ENDCG
	}
	FallBack "Diffuse"
}
