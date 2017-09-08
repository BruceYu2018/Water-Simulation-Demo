    #define pi 3.14159265358979
    #define MAX_NUM_OF_WAVE 10 //maximum number of waves added up to

    struct appdata{
        float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
    };

    struct v2f {
        float2 uv_OceanTex : TEXCOORD0;
		float4 uv_SmallWaveBump : TEXCOORD1;
	    float3 screenPos : TEXCOORD2;
	    float3 worldPos : TEXCOORD3;
        float3 normal : TEXCOORD4;
        float3 tangent : TEXCOORD5;
        float3 binormal : TEXCOORD6;
        float4 vertex : SV_POSITION;
	};

	sampler2D _OceanTex;
	sampler2D _SmallWaveBump;
	sampler2D _OceanFloor;
    sampler2D _iWavePosTex;
    sampler2D _iWaveNormal;

    // OceanTex
	fixed4 _Color;

    //shallow wave parameters
    int num_of_wave_SW;
    float2 direction_SW[MAX_NUM_OF_WAVE];
    float waterDepth_SW[MAX_NUM_OF_WAVE];
    float amplitude_SW[MAX_NUM_OF_WAVE];
    float velocity_SW[MAX_NUM_OF_WAVE];
    float wavelength_SW[MAX_NUM_OF_WAVE];
    float WVT_SW[MAX_NUM_OF_WAVE]; //wavelength variance tolerance
    float steepness_SW[MAX_NUM_OF_WAVE];
    float SVT_SW[MAX_NUM_OF_WAVE]; //steepness variance tolerance

    //gerstner wave parameters
    int num_of_wave_GW;
    float2 direction_GW[MAX_NUM_OF_WAVE];
    float amplitude_GW[MAX_NUM_OF_WAVE];
    float velocity_GW[MAX_NUM_OF_WAVE];
    float wavelength_GW[MAX_NUM_OF_WAVE];
    float steepness_GW[MAX_NUM_OF_WAVE];

    //reflection
    samplerCUBE _ReflCubemap;
    sampler2D _ReflTex;
    float _ReflAmount;
    float _ReflDistorAmount;

    //refraction
    sampler2D _RefraTex;
    float _RefraAmount;
    float _RefraDistorAmount;
    float4x4 _RefracCameraVP;

    //bump map 
    float _BumpStrength;
    float4 _BumpDirection;
    float4 _BumpTiling;

    //Scattering
    float _ScatteringKd;
	float _ScatteringAtten;
	float _ScatteringDiffuse;
	float _depthColorScale;

    //light
    float _Shininess;

    half SchlickFresnel(float3 V, float3 N, float R0) {
    	//Schlick's approximation fresnel factor
        float cos_theta = dot(V, N);

        return R0 +(1-R0)*pow(1-cos_theta,5);    
    }

    float3 adjustedBump(sampler2D bumpMap, float4 uv, float bumpStrength) {
        float3 bump = (UnpackNormal(tex2D(bumpMap, uv.xy)) + UnpackNormal(tex2D(bumpMap,uv.zw))) * 0.5;
        bump += (UnpackNormal(tex2D(bumpMap, uv.xy*2))*0.5 + UnpackNormal(tex2D(bumpMap,uv.zw*2))*0.5) * 0.5;
        bump += (UnpackNormal(tex2D(bumpMap, uv.xy*8))*0.5 + UnpackNormal(tex2D(bumpMap,uv.zw*8))*0.5) * 0.5;
        float3 normal = float3(0,0,0);
        normal.xz= bump.xy * bumpStrength;
        normal.y = 1;

        return normal;
    }

    float calShallowWaveHeight(float3 v, int num, float floorDepth, float steepness){
    	float height = 0.0;
        float depth = waterDepth_SW[num];

        #if defined(OCEAN_FLOOR_ON)
        	depth = v.y-floorDepth;
        #endif

        if (depth > 0) {
            float phaseConstant = (2.0*pi*velocity_SW[num])/wavelength_SW[num];
            float scaleFactor = (2*depth/wavelength_SW[num]-1)+(10*max(0.15,depth)-depth);
            float adjustedWavelength = wavelength_SW[num] + WVT_SW[num]*scaleFactor;
            float adjustedSteepness = steepness + (1+SVT_SW[num])*(2*depth/steepness-1);

            float tempResult = (2.0*pi*dot(float3(direction_SW[num].x, 1.0, direction_SW[num].y), v))/adjustedWavelength;
            height = sin(tempResult + phaseConstant*_Time.x);
            height = amplitude_SW[num]*pow((height+1.0)/2.0, adjustedSteepness);
        }

        return height;
    }

    float3 partialDerivativeSW(float3 v, int num, float floorDepth){
        float3 result = float3(0,0,0);

        result.x = calShallowWaveHeight(v, num, floorDepth, steepness_SW[num]-1) * steepness_SW[num];
        result.z = calShallowWaveHeight(v, num, floorDepth, steepness_SW[num]-1) * steepness_SW[num];

        float cosWave = 0.0;
        float depth = waterDepth_SW[num];

        #if defined(OCEAN_FLOOR_ON)
            depth = v.y-floorDepth;
        #endif

        float phaseConstant = (2.0*pi*velocity_SW[num])/wavelength_SW[num];
        float scaleFactor = (2*depth/wavelength_SW[num]-1)+(10*max(0.15,depth)-depth);
        float adjustedWavelength = wavelength_SW[num] + WVT_SW[num]*scaleFactor;
        if (depth > 0) {
            float tempResult = (2.0*pi*dot(float3(direction_SW[num].x, 1.0, direction_SW[num].y), v))/adjustedWavelength;
            cosWave = cos(tempResult + phaseConstant*_Time.x);
        }

        result.x *= 0.5 * cosWave;
        result.x *= (2.0*pi*direction_SW[num].x)/adjustedWavelength;
        result.z *= 0.5 * cosWave;
        result.z *= (2.0*pi*direction_SW[num].y)/adjustedWavelength;

        return result;
    }

    float3 calGerstnerWave(float3 v, int num, int totalNum){
        float3 result = float3(0,0,0);
        float omega = (2*pi) / wavelength_GW[num];
        float phase = velocity_GW[num] * omega;
        float q = steepness_GW[num] / (omega * amplitude_GW[num] * totalNum);

        // avoid dividing by zero and compute result
        if (amplitude_GW[num] == 0) q = steepness_GW[num] / (omega * totalNum); 
        if (wavelength_GW[num] == 0) omega = 0;
        result.x = q*amplitude_GW[num]*direction_GW[num].x*cos(omega*dot(direction_GW[num], v.xz)+phase*_Time.x);
        result.z = q*amplitude_GW[num]*direction_GW[num].y*cos(omega*dot(direction_GW[num], v.xz)+phase*_Time.x);
        result.y = amplitude_GW[num]*sin(omega*dot(direction_GW[num], v.xz)+phase*_Time.x);

        return result;
    }

    void partialDerivativeGW(float3 v, int num, int totalNum, out float3 tangent, out float3 binormal, out float3 normal){
        float omega = (2*pi) / wavelength_GW[num];
        float phase = velocity_GW[num] * omega;
        float q = steepness_GW[num] / (omega * amplitude_GW[num] * totalNum);

        if (amplitude_GW[num] == 0) q = steepness_GW[num] / (omega * totalNum); 
        if (wavelength_GW[num] == 0) omega = 0;
        float SIN = sin(omega*dot(direction_GW[num], v.xz)+phase*_Time.x);
        float COS = cos(omega*dot(direction_GW[num], v.xz)+phase*_Time.x);

        tangent = float3(0,0,0);
        tangent.x = 1-q*direction_GW[num].x*direction_GW[num].x*omega*amplitude_GW[num]*SIN;
        tangent.z = -q*direction_GW[num].x*direction_GW[num].y*omega*amplitude_GW[num]*SIN;
        tangent.y = direction_GW[num].x*omega*amplitude_GW[num]*COS;

        binormal = float3(0,0,0);
        binormal.x = -q*direction_GW[num].x*direction_GW[num].y*omega*amplitude_GW[num]*SIN;
        binormal.z = 1-q*direction_GW[num].y*direction_GW[num].y*omega*amplitude_GW[num]*SIN;
        binormal.y = direction_GW[num].y*omega*amplitude_GW[num]*COS;

        normal = float3(0,0,0);
        normal.x = -1.0*tangent.y;
        normal.z = -1.0*binormal.y;
        normal.y = 1-q*omega*amplitude_GW[num]*SIN;

    }

    v2f vert (appdata v) {
        v2f o;

        //init
        float3 v_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        float3 tangent = float3(0,0,1);
        float3 binormal = float3(1,0,0);
        float3 normal = float3(0,0,0);
        float3 vTemp = float3(v_worldPos.x, 0, v_worldPos.z);

        //shallow wave caculation
        //sampling the terrain height texture to get the ocean floor terrain height (world position)
    	float floorDepth = tex2Dlod(_OceanFloor, float4(v.texcoord,0,0)).y;      
        float3 temp;
    	for (int i = 0; i < num_of_wave_SW; i++){
	    	vTemp.y += calShallowWaveHeight(v_worldPos, i, floorDepth, steepness_SW[i]);
            temp = partialDerivativeSW(v_worldPos, i, floorDepth);
            tangent.y += temp.x;
            binormal.y += temp.z;
        }
        normal = normalize(float3(-1.0*tangent.y, 1, -1.0*binormal.y));

        //gerstner wave caculation
        float3 tempTangent, tempBinormal, tempNormal;
        float3 tempNormalTotal = float3(0,0,0);
        for (i = 0; i < num_of_wave_GW; i++){
            vTemp += calGerstnerWave(v_worldPos, i, num_of_wave_GW);
            partialDerivativeGW(v_worldPos, i, num_of_wave_GW, tempTangent, tempBinormal, tempNormal);
            tangent += tempTangent;
            binormal += tempBinormal;
            tempNormalTotal += tempNormal;
        }
        normal += normalize(tempNormalTotal);

        //add interactive wave
        float4 iWaveVertex = tex2Dlod(_iWavePosTex, float4(v.texcoord, 0, 0));
        float4 iWaveNormal = tex2Dlod(_iWaveNormal, float4(v.texcoord, 0, 0));
        vTemp.y += iWaveVertex.y;
        normal += iWaveNormal.xyz;
        
        v_worldPos = vTemp;
        v.vertex.xyz = mul((float3x3)unity_WorldToObject, v_worldPos);

        o.vertex= UnityObjectToClipPos(v.vertex);
        o.uv_OceanTex = v.texcoord;
        o.uv_SmallWaveBump = v_worldPos.xzxz + _Time.yyyy * _BumpDirection.xyzw;
        o.screenPos = ComputeScreenPos(o.vertex).xyw;
        o.worldPos = v_worldPos;
        o.normal = normalize(normal);
        o.tangent = normalize(tangent);
        o.binormal = normalize(binormal);

        return o;
    }

    float4 frag (v2f i) : SV_Target {
        //pick up ocean texture as color
        fixed4 color = tex2D (_OceanTex, i.uv_OceanTex)*_Color;

        //add bump texture       
        i.uv_SmallWaveBump *= _BumpTiling;
        float3 bumpNormal = adjustedBump(_SmallWaveBump, i.uv_SmallWaveBump, _BumpStrength);
        float3x3 M = {i.tangent, i.normal, i.binormal}; //matrix from world space to tangent space
        float3 totalNormal = normalize(mul(transpose(M), normalize(bumpNormal))); //map bump nomral from tangent space to world space
        //float3 totalNormal = i.normal;
        
        //mirror reflection, pick up reflection texture and add distortion
        float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
        float2 distortion = totalNormal.xz*viewDir.y;
        float2 screenUV = i.screenPos.xy/i.screenPos.z;
        half4 reflectionColor = tex2D (_ReflTex, screenUV + distortion*_ReflDistorAmount);

        //cubmap refleciton
        float3 skyColor = texCUBE (_ReflCubemap, reflect(-viewDir, totalNormal)).rgb;
        reflectionColor.xyz= lerp(skyColor, reflectionColor.xyz, reflectionColor.a)*_ReflAmount;

        //refraction and scattering, and use water depth to adjust the transparent of refraction color
        half4 refractionColor = tex2D (_RefraTex, screenUV+distortion*_RefraDistorAmount)*_RefraAmount;
        float3 floorPos = tex2D (_OceanFloor, i.uv_OceanTex).xyz;
        float floorToCamLength = length(_WorldSpaceCameraPos.xyz-floorPos);
        float depth = max(0,i.worldPos.y-floorPos.y);
        float floorToCamHeight = _WorldSpaceCameraPos.y-floorPos.y;
        float floorToWaterLength = floorToCamLength*depth/floorToCamHeight;
        float scatteringOut = exp(-_ScatteringAtten*floorToWaterLength);
        float scatteringIn = _ScatteringDiffuse*(1-scatteringOut*exp(depth*_ScatteringKd));
        refractionColor = (refractionColor*_Color*scatteringOut+scatteringIn)*floorPos.y*_depthColorScale;

        //blend color
        float fresnel = SchlickFresnel(viewDir, totalNormal, 0.02f);
        color.xyz += lerp(refractionColor.xyz, reflectionColor.xyz, fresnel);
        color.a = reflectionColor.a;

        //Simple Blinn-Phong light
        half3 h = normalize(normalize(_WorldSpaceLightPos0.xyz) + viewDir);
        half diff = max (0, dot (totalNormal, normalize(_WorldSpaceLightPos0.xyz)));
        float nh = max (0, dot (totalNormal, h));
        float spec = pow (nh, _Shininess);
        color.xyz = saturate(color.xyz * diff + spec);

		return color;
	}