Shader "demoShaders/GerstnerWave"
{
	Properties{
	    _Tess ("Tessellation", Range(1,32)) = 4
		_MainTex("Texture", 2D) = "white" {}
	    _MainColor("Main Color", Color) = (0,0.8,1,0.5)
		_QA("Q(Q1,Q2,Q3,Q4)", Vector)=(1,1,1,1)
		_A("A(A1,A2,A3,A4)", Vector)=(1,1,1,1)
		_Dx("Direction x component (Dx1,Dx2,Dx3,Dx4)", Vector)=(1,1,1,1)
		_Dz("Direction z component (Dz1,Dz2,Dz3,Dz4)", Vector)=(1,1,1,1)
		_S("Speed(S1,S2,S3,S4)", Vector)=(1,1,1,1)
		_L("Length(L1,L2,L3,L4)", Vector)=(1,1,1,1)
		_Smoothing("Diffuse Smoothing", Float)= 0
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Tags { "LightMode" = "ForwardBase" }
		LOD 100
	    
		Pass 
		{
			cull off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag	
			#pragma target 5.0
			// include unityCG package
			#include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0
            #include "Tessellation.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				fixed4 diff : COLOR0; // diffuse lighting color
			};
			
			sampler2D _MainTex;
			float4 _MainColor;
			float4 _MainTex_ST;
			float4 _QA;
			float4 _A;
			float4 _S;
			float4 _Dx;
			float4 _Dz;
			float4 _L;
			float _Smoothing;
			
			float3 CalculateWavesDisplacement(float3 vert)
			{
				float3 pos = float3(0,0,0);

				float4 phase = _Dx*vert.x+_Dz*vert.z+_S*_Time.x;
				float4 sinp=float4(0,0,0,0), cosp=float4(0,0,0,0);
				sincos(_L*phase, sinp, cosp);

				pos.x = dot(_QA*_Dx, cosp);
				pos.z = dot(_QA*_Dz, cosp);
				pos.y = dot(_A, sinp);

				return pos;
			}

			v2f vert (appdata v)
			{
				v2f o;				
			    float3 worldPos0 = mul(unity_ObjectToWorld, v.vertex);
				float3 worldPos1 = worldPos0 + float3(0.05, 0, 0);
				float3 worldPos2 = worldPos0 + float3(0, 0, 0.05);
				// Get new vertex after displacement
				float3 disPos0 = CalculateWavesDisplacement(worldPos0);
				float3 v0 = worldPos0 + disPos0;
				float3 disPos1 = CalculateWavesDisplacement(worldPos1);
				float3 v1 = worldPos1 + disPos1;
				float3 disPos2 = CalculateWavesDisplacement(worldPos2);
				float3 v2 = worldPos2 + disPos2;

				v1.y -= (v1.y - v0.y)*_Smoothing;
				v2.y -= (v2.y - v0.y)*_Smoothing;
                // Caculate new normal
				float3 newNormal = cross(v2 - v0, v1 - v0);

				// Update vertex and normal
				v.vertex.xyz = mul(unity_WorldToObject, float4(v0, 1));
				v.normal = normalize(mul(unity_WorldToObject, newNormal));
				// Transfer vertex to screen space
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				// get vertex normal in world space
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				// dot product between normal and light direction for
				// standard diffuse (Lambert) lighting
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				// factor in the light color
				o.diff = nl * _LightColor0;
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _MainColor;
			    col *= i.diff;
				return col;
			}
			ENDCG
		}
	}
}