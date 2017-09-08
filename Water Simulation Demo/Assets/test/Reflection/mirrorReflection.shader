Shader "testShaders/mirrorReflection"
{
	Properties
	{
		_RefColor ("Reflection Color", Color) = (1,1,1,1)
		_RefTexture ("Reflection Texture", 2D) = "white" {}
	    _RefRatio ("Refleciton Ratio", Range(0.01, 1)) = 0.5
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
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
			};

			fixed4 _RefColor;
			float _RefRatio;
			sampler2D _RefTexture;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.screenPos = ComputeScreenPos(o.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _RefColor;
				half4 reflectionColor = tex2D(_RefTexture, i.screenPos.xy/i.screenPos.w) * _RefRatio;
				col += reflectionColor;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
