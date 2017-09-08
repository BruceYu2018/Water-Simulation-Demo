Shader "testShaders/MyParticleUpdate"
{
	Properties{
		_PosTex("Position Texture", 2D) = "white" {}
	    _VelTex("Velocity Texture", 2D) = "white" {}
	    _AccTex("Acceleration Texture", 2D) = "white" {}
	}

	SubShader{

		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#include "UnityCG.cginc"

		struct appdata {
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		struct f2o {
			float4 color0 : COLOR0;
			float4 color1 : COLOR1;
			float4 color2 : COLOR2;
		};

		v2f vert(appdata IN) {
			v2f o;
			o.vertex = UnityObjectToClipPos(IN.vertex);
			o.uv = IN.uv;
			return o;
		}

		sampler2D _PosTex;
		sampler2D _VelTex;
		sampler2D _AccTex;

		f2o init(v2f IN) {
			f2o OUT;

			OUT.color0 = float4(0, 0, 0, 1);
			OUT.color1 = float4(0, 0, 0, 1);
			OUT.color2 = float4(0, 0, 0, 1);

			return OUT;
		}

		f2o update(v2f IN) {
			f2o OUT;

			float4 pos = tex2D(_PosTex, IN.uv);
			float4 vel = tex2D(_VelTex, IN.uv);
			float4 acc = tex2D(_AccTex, IN.uv);

			acc.x = 0.1;
			vel.x += acc.x;
			pos.x += vel.x * unity_DeltaTime.x;

			OUT.color0 = pos;
			OUT.color1 = vel;
			OUT.color2 = acc;

			return OUT;
		}

		ENDCG

		Pass{
			CGPROGRAM
	            #pragma vertex vert
	            #pragma fragment init
			ENDCG
		}

		Pass{
			CGPROGRAM
	            #pragma vertex vert
	            #pragma fragment update
			ENDCG
		}

	}
}
