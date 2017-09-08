Shader "testShaders/SPHUpdate"
{
	Properties{
    _PosTex ("Position Texture", 2D) = "white" {}
    _VelTex ("Velocity Texture", 2D) = "white" {}
    _AccTex ("Acceleration Texture", 2D) = "white" {}

    // particle paramter
    _DensityTex ("Density Texture", 2D) = "white" {}
    _PressureTex ("Pressure Texture", 2D) = "white" {}
    _mass ("Particle Mass", float) = 1
    _h ("Kernel Function Radius", float) = 3
    _mu ("Fluid Viscosity", float) = 0.005
    _l ("Thereshold Parameter", float) = 1
    _sigma ("Tension Coefficient", float) = 0.1
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
		sampler2D _VelTex;
		sampler2D _AccTex;
		sampler2D _DensityTex;
		sampler2D _PressureTex;
        
    float _mass;
    float _h;
    float _mu;
    float _l;
    float _sigma;
		float4 _PressureTex_TexelSize;

        float densityKernel(float r, float h){
        	//Debrun's spiky kernal
        	float w;
        	if (r > 0 && r <= h)
        	    w = 315/(64*pi*pow(h,9))*pow((pow(h,2)-pow(r,2)),3);
        	else 
        	    w = 0;
            
            return w;
        }

        float dPressureKernel(float r, float h){
        	float w;
        	if (r > 0 && r <= h)
        	    w = -45/(pi*pow(h,6))*(pow(h-r,2));
        	else
        	    w = 0;

        	return w;
        }

        float ddViscosityKernel(float r, float h){
        	float w;
        	if (r > 0 && r <= h)
        	    w = 45/(pi*pow(h,6))*(h-r);
        	else
        	    w = 0;

        	return w;
        }

        float dDensityKernel(float r, float h){
        	float w;
        	if (r > 0 && r <= h)
        	    w = 945/(64*pi*pow(h,9))*pow(h*h-r*r,2)*(-2*h);
        	else 
        	    w = 0;
            
            return w;
        }

        float ddDensityKernel(float r, float h){
        	float w;
        	if (r > 0 && r <= h)
        	    w = -945/(32*pi*pow(h,9))*(pow(h,4)-6*h*h*r*r+4*pow(r,5));
        	else 
        	    w = 0;
            
            return w;
        }

		f2o frag(v2f IN) {
			f2o OUT;

			float4 pos = tex2D(_PosTex, IN.uv);
			float4 vel = tex2D(_VelTex, IN.uv);
			float4 acc = tex2D(_AccTex, IN.uv);
			float currentDensity = tex2D(_DensityTex, IN.uv).x;
			float currentPressure = tex2D(_PressureTex, IN.uv).x;
            
      float size = _PressureTex_TexelSize.z;
      float radius;
      float2 uvOffset;
      float3 tempv;       
      float3 direction;
      float3 Fp;
      float3 Fv;
      float3 Fsurf;
      float3 Fe;
      float colorField;
      float3 dColorField;
      float norm_dColorField;
      float ddColorField;
      for (int x = 0; x < size; x++){
         for (int y = 0; y < size; y++){
         	   uvOffset.x = (x + 0.5) * _PressureTex_TexelSize.x;
         	   uvOffset.y = (y + 0.5) * _PressureTex_TexelSize.y;
         	   tempv = tex2D(_PosTex, uvOffset).xyz;
         	   radius = distance(pos.xyz, tempv);
             direction = pos.xyz - tempv;
             // pressure
             float3 dw = dPressureKernel(radius, _h)*direction;
             float pressure = tex2D(_PressureTex, uvOffset).x;
             float density = tex2D(_DensityTex, uvOffset).x;
             Fp += _mass*((currentPressure+pressure)/(2*density))*dw;
             // viscosity
             float ddw = ddViscosityKernel(radius, _h);
             float4 nvel = tex2D(_VelTex, uvOffset);
             Fv += _mass*((vel.xyz+nvel.xyz)/density)*ddw;
             // surface tension
             colorField += (_mass/density)*densityKernel(radius, _h);
             dw = dDensityKernel(radius, _h)*direction;
             dColorField += (_mass/density)*dw;
             norm_dColorField = length(dColorField);
             ddw = ddDensityKernel(radius, _h);
             ddColorField += (_mass/density)*ddw;
             if (norm_dColorField > 0.5)
                Fsurf = -_sigma*ddColorField*(dColorField/norm_dColorField);
             else
                Fsurf = float3(0,0,0);
         }
      }
      Fe = float3(0,-9.8,0);
      float3 F = -Fp+_mu*Fv+currentDensity*Fe+Fsurf;

      vel.xyz += 0.01*(F/currentDensity);
      if (vel.x <-10 || vel.x > 40 || vel.y < -10 || vel.y > 40|| vel.z < -10 || vel.z > 40)
            vel.xyz = -vel.xyz;

      pos.xyz += 0.01*vel.xyz;

			OUT.color0 = pos;
			OUT.color1 = vel;
			OUT.color2 = acc;

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
