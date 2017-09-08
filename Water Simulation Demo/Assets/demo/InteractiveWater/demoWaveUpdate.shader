Shader "demoShaders/demoWaveUpdate"
{
	Properties
	{
		_currentHeight ("Current Height", 2D) = "black" {} 
	    _previousHeight ("Previous Height", 2D) = "black" {}
	    _data ("Data", 2D) = "black" {}
	    _kernel ("Kernel", 2D) = "black" {}
	    _alpha("Alpha", Range(0,1)) = 0.9

	}
	SubShader
	{
	    Cull Off ZWrite Off ZTest Always

		CGINCLUDE

		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		struct f2o {
			float4 color0 : COLOR0;
			float4 color1 : COLOR1;
			float4 color2 : COLOR2;
		};

		sampler2D _currentHeight;
		sampler2D _previousHeight;
		sampler2D _data;
		sampler2D _kernel;

		float _alpha;
		float kernelSize;
		float4 _currentHeight_TexelSize;
		float4 _kernel_TexelSize;
		float4 _currentHeight_ST;

        float3 calculateNormal(float2 uv){
            float3 current_v = tex2D(_currentHeight, uv).xyz;
            float3 leftTop_v = tex2D(_currentHeight, float2(uv.x-_currentHeight_TexelSize.x, uv.y-_currentHeight_TexelSize.y)).xyz;
            float3 top_v = tex2D(_currentHeight, float2(uv.x, uv.y-_currentHeight_TexelSize.y)).xyz;
            float3 rightTop_v = tex2D(_currentHeight, float2(uv.x+_currentHeight_TexelSize.x, uv.y-_currentHeight_TexelSize.y)).xyz;
            float3 left_v = tex2D(_currentHeight, float2(uv.x-_currentHeight_TexelSize.x, uv.y)).xyz;
            float3 right_v = tex2D(_currentHeight, float2(uv.x+_currentHeight_TexelSize.x, uv.y)).xyz; 
            float3 leftBot_v = tex2D(_currentHeight, float2(uv.x-_currentHeight_TexelSize.x, uv.y+_currentHeight_TexelSize.y)).xyz;
            float3 bot_v = tex2D(_currentHeight, float2(uv.x, uv.y+_currentHeight_TexelSize.y)).xyz;
            float3 rightBot_v = tex2D(_currentHeight, float2(uv.x+_currentHeight_TexelSize.x, uv.y+_currentHeight_TexelSize.y)).xyz;

            float3 normal = float3 (0,0,0);
            if (current_v.y == 0 && leftTop_v.y == 0 && top_v.y == 0 && rightTop_v.y == 0 && leftBot_v.y == 0  
            	&& left_v.y == 0 && right_v.y== 0 && bot_v.y == 0 && rightBot_v.y == 0){
            	normal = float3(0,1,0);
            } else {
		            normal = cross(leftTop_v-current_v, left_v-current_v);
		            normal += cross(top_v-current_v, leftTop_v-current_v);
		            normal += cross(right_v-current_v, top_v-current_v);
		            normal += cross(top_v-rightTop_v, right_v-rightTop_v);
		            normal += cross(rightBot_v-current_v, right_v-current_v);
		            normal += cross(bot_v-current_v, rightBot_v-current_v);
		            normal += cross(left_v-current_v, bot_v-current_v);
		            normal += cross(bot_v-leftBot_v, left_v-leftBot_v);
		            normal = normal/6;
            }

        	return normalize(normal);
        }

        float convolution(float2 uv){
            //convolution of vertical derivative
            float2 uvOffset;
            float2 kernelOffset;
            float kernelValue;
            float tempHeight;
            float verticalDerivative = 0.0;
            for (int x = -kernelSize; x <= kernelSize; ++x){
            	for (int y = -kernelSize; y <= kernelSize; ++y){
            		uvOffset = uv;
            		uvOffset.x += x * _currentHeight_TexelSize.x;
            		uvOffset.y += y * _currentHeight_TexelSize.y;

                    kernelOffset.x = (x + kernelSize + 0.5) * _kernel_TexelSize.x;
                    kernelOffset.y = (y + kernelSize + 0.5) * _kernel_TexelSize.y;
            		kernelValue = tex2D(_kernel, kernelOffset).x;

                    if (uvOffset.x >= 0 && uvOffset.x <= 1 && uvOffset.y >=0 && uvOffset.y <= 1){
                    	tempHeight = tex2D(_currentHeight, uvOffset).y;
            		    } else {
            		    	if (uvOffset.x > 1 && uvOffset.y >=0 && uvOffset.y <= 1){	
            		    		tempHeight = tex2D(_currentHeight, float2(2-uvOffset.x, uvOffset.y)).y;
            		    	} 
            		    	else if (uvOffset.x < 0 && uvOffset.y >=0 && uvOffset.y <= 1){
            		    		tempHeight = tex2D(_currentHeight, float2(-uvOffset.x, uvOffset.y)).y;
            		    	}
            		    	else if (uvOffset.y > 1 && uvOffset.x >=0 && uvOffset.x <= 1){
                                tempHeight = tex2D(_currentHeight, float2(uvOffset.x, 2-uvOffset.y)).y;
            		    	}
            		    	else if (uvOffset.y < 0 && uvOffset.x >=0 && uvOffset.x <= 1){
                                tempHeight = tex2D(_currentHeight, float2(uvOffset.x, -uvOffset.y)).y;
            		    	}
            		    }
            	    verticalDerivative += tempHeight * kernelValue;
            	}
            }

            return verticalDerivative; 
        }

        float4 updateHeight(float4 currentHeight, float4 previousHeight, float verticalDerivative, float4 data)
        {
        	float dt = 0.02;
            float g = -9.81;
            float newHeight = 0;
            float4 newPos = currentHeight;
            
            float source = data.x;
            float obstruction = data.y;

            float twoMinusAlphaTimesDt = 2 - _alpha * dt;
            float onePlusAlphaTimesDt = 1 + _alpha * dt;
            float gravityTimesDtTimesDt = g * dt * dt;
            float height = (currentHeight.y + source)*obstruction;
            
            newHeight += height * twoMinusAlphaTimesDt;
            newHeight -= previousHeight.y;
            newHeight -= gravityTimesDtTimesDt * verticalDerivative;
            newHeight /= onePlusAlphaTimesDt;

            newPos.y = newHeight;
            return newPos;
        }

		v2f vert (appdata v)
		{
			v2f o;  
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = TRANSFORM_TEX(v.uv, _currentHeight);

			return o;
		}
		
		f2o update (v2f i)
		{
			f2o OUT;
			
			float4 data = tex2D(_data, i.uv);
            float4 currentHeight = float4(data.z, tex2D(_currentHeight, i.uv).y, data.w, 0);
            float4 currentNormal = float4(calculateNormal(i.uv).xyz, 1);
			float4 previousHeight = tex2D(_previousHeight, i.uv);
            float verticalDerivative = convolution(i.uv);
			float4 newPos = updateHeight(currentHeight, previousHeight, verticalDerivative, data);

		    OUT.color0 = currentHeight;
			OUT.color1 = newPos;
			OUT.color2 = currentNormal;

			return OUT;
		}

		ENDCG

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment update
			ENDCG
		}
	}
}
