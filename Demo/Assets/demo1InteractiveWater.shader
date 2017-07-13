Shader "Unlit/demo1InteractiveWater"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	    _previousHeight("Previous Height", 2D) = "black"{}
	    _verticalDerivative("Previous Height", 2D) = "black"{}
	    _sourceAndObstruction("Previous Height", 2D) = "black"{}
	    _alpha("Alpha", Range(0,1)) = 0.9
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
			};

			sampler2D _MainTex, _previousHeight;
			sampler2D _verticalDerivative, _sourceAndObstruction;
			float4 _MainTex_ST;
			float _alpha;
			float _waterWidth;
			float _gridSpacing;
			float4 data1, data2, data3;

            float updateHeight(float3 vert, fixed4 data1, fixed4 data2, fixed4 data3)
            {
            	float dt = 0.02;
                float g = -9.81;
                float newHeight = 0;
                
                if (data1.g==0)
                    data1.g==-1;
                if (data2.g==0)
                	data2.g==-1;
                if (data3.g==0)
                	data3.g=-1;

                float previousHeight = data1.r * data1.g * data1.b;
                float verticalDerivative = data2.r * data2.g * data2.b;
                float source = data3.r * data3.g * data3.b;
                float obstruction = data3.a;

                float twoMinusAlphaTimesDt = 2 - _alpha * dt;
                float onePlusAlphaTimesDt = 1 + _alpha * dt;
                float gravityTimesDtTimesDt = g * dt * dt;
                float height = (vert.y+source)*obstruction;
                
                newHeight += height * twoMinusAlphaTimesDt;
                newHeight -= previousHeight;
                newHeight -= gravityTimesDtTimesDt * verticalDerivative;
                newHeight /= onePlusAlphaTimesDt;

                return newHeight;
            }

			v2f vert (appdata v)
			{
				v2f o;
				data1 = tex2Dlod(_previousHeight, float4(v.uv,0,0));
				data2 = tex2Dlod(_verticalDerivative, float4(v.uv,0,0));
				data3 = tex2Dlod(_sourceAndObstruction, float4(v.uv,0,0));
				v.vertex.y = updateHeight(v.vertex.xyz, data1,data2,data3);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);

				return col;
			}
			ENDCG
		}
	}
}
