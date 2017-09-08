Shader "testShaders/SPHDensity"
{
	Properties{
		_PosTex("Position Texture", 2D) = "white" {}

	    // particle paramter
	    _mass ("Particle Mass", float) = 1
	    _h ("Kernel Function Radius", float) = 3
	}

	SubShader{

		Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#include "UnityCG.cginc"
        #define pi 3.14159265358979

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
        
        float _mass;
        float _h;
		float4 _PosTex_TexelSize;

        float densityKernel(float r, float h){
        	//Debrun's spiky kernal
        	float w;
        	if (r > 0 && r <= h*h)
        	    w = 315/(64*pi*pow(h,9))*pow((pow(h,2)-r),3);
        	else 
        	    w = 0;
            
            return w;
        }

		f2o frag(v2f IN) {
			f2o OUT;

			float4 pos = tex2D(_PosTex, IN.uv);
            
            // compute density
            int k = 3;
            float restDensity = 0.8139;
            float size = _PosTex_TexelSize.z;
            float radius;
            float2 uvOffset;
            float3 tempv;
            float density = 0;
            for (int x = 0; x < size; x++){
               for (int y = 0; y < size; y++){
               	   uvOffset.x = (x + 0.5) * _PosTex_TexelSize.x;
               	   uvOffset.y = (y + 0.5) * _PosTex_TexelSize.y;
		           tempv = tex2D(_PosTex, uvOffset).xyz;
		           radius = pow(distance(pos.xyz, tempv), 2);
                   density += _mass*densityKernel(radius, _h);               
               }
            }
            if (density == 0) density = 0.001;
            float pressure = k*(density-restDensity);          
            
			OUT.color0 = float4(density,0,0,1);
			OUT.color1 = float4(pressure,0,0,1);
			OUT.color2 = float4(0,0,0,1);

			return OUT;
		}

		ENDCG

		Pass{
			CGPROGRAM
	            #pragma vertex vert
	            #pragma fragment frag
			ENDCG
		}

	}
}
