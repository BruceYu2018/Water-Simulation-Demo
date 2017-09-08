    #include "Tessellation.cginc"
    #define pi 3.14159265358979
    #define MAX_NUM_OF_WAVE 10 //maximum number of waves added up to

    struct appdata{
        float4 vertex : POSITION;
		float4 tangent : TANGENT;
		float3 normal : NORMAL;
		float2 texcoord : TEXCOORD0;
		float2 texcoord1 : TEXCOORD1;
		float2 texcoord2 : TEXCOORD2;
    };

	sampler2D _OceanTex;
	sampler2D _SmallWaveBump;
	sampler2D _OceanFloor;

	fixed4 _Color;

    //wave parameters
    float2 direction[MAX_NUM_OF_WAVE];
    float waterDepth[MAX_NUM_OF_WAVE];
    float amplitude[MAX_NUM_OF_WAVE];
    float velocity[MAX_NUM_OF_WAVE];
    float wavelength[MAX_NUM_OF_WAVE];
    float WVT[MAX_NUM_OF_WAVE]; //wavelength variance tolerance
    float steepness[MAX_NUM_OF_WAVE];
    float SVT[MAX_NUM_OF_WAVE]; //steepness variance tolerance
    float _DepthScale;
    int num_of_wave; //number of waves added up to

    //reflection
    samplerCUBE _ReflCubemap;
    sampler2D _ReflTex;
    float _ReflAmount;
    float _ReflDistorAmount;

    //bump map 
    float _BumpStrength;
    float4 _BumpDirection;
    float4 _BumpTiling;

    //tessellation
    float _Tess;
    float _minDist;
    float _maxDist;

	struct Input {
		float2 uv_OceanTex : TEXCOORD0;
		float2 uv_SmallWaveBump : TEXCOORD1;
        float4 screenPos;
        float3 viewDir;
        float3 worldRefl;
        float3 worldNormal;
        //float3 worldPos;
        INTERNAL_DATA
	};

    // float4 tessDistance(appdata v0, appdata v1, appdata v2){
    //     float4 tess;
    //     #if defined(TESS_ON)
    //         tess = UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, _minDist, _maxDist, _Tess);
    //     #else 
    //         tess = float4(1,1,1,1);
    //     #endif

    //     return tess; 
    // }

    float3 adjustedBump(sampler2D bumpMap, float4 uv, float bumpStrength) {
        float3 bump = (UnpackNormal(tex2D(bumpMap, uv.xy)) + UnpackNormal(tex2D(bumpMap,uv.zw))) * 0.5;
        bump += (UnpackNormal(tex2D(bumpMap, uv.xy*2))*0.5 + UnpackNormal(tex2D(bumpMap,uv.zw*2))*0.5) * 0.5;
        bump += (UnpackNormal(tex2D(bumpMap, uv.xy*4))*0.5 + UnpackNormal(tex2D(bumpMap,uv.zw*4))*0.5) * 0.5;
        bump.y = bump.y * bumpStrength;

        return bump;
    }

    float caculateWaveHeight(float3 v, int num, float floorDepth, float steepness){
    	float height = 0.0;
        float depth = waterDepth[num];

        #if defined(OCEAN_FLOOR_ON)
        	depth = _DepthScale*(v.y-floorDepth);
            //if depth is too small the water height would be unstable
            //if ( 0.0 < depth/_DepthScale && depth/_DepthScale < 0.4) depth = 0.4 * _DepthScale;
        #endif

        if (depth > 0) {
            float phaseConstant = (2.0*pi*velocity[num])/wavelength[num];
            float scaleFactor = (2*depth/wavelength[num]-1)+(10*max(0.15,depth)-depth);
            float adjustedWavelength = wavelength[num] + WVT[num]*scaleFactor;
            float adjustedSteepness = steepness + (1+SVT[num])*(2*depth/steepness-1);

            float tempResult = (2.0*pi*dot(float3(direction[num].x, 1.0, direction[num].y), v))/adjustedWavelength;
            height = sin(tempResult + phaseConstant*_Time.x);
            height = amplitude[num]*pow((height+1.0)/2.0, adjustedSteepness);
        }

        return height;
    }

    float3 partialDerivative(float3 v, int num, float floorDepth){
        float3 result = float3(0,0,0);

        result.x = caculateWaveHeight(v, num, floorDepth, steepness[num]-1) * steepness[num];
        result.z = caculateWaveHeight(v, num, floorDepth, steepness[num]-1) * steepness[num];

        float cosWave = 0.0;
        float depth = waterDepth[num];

        #if defined(OCEAN_FLOOR_ON)
            depth = _DepthScale*(v.y-floorDepth);
            //if depth is too small the water height would be unstable
            if ( 0.0 < depth/_DepthScale && depth/_DepthScale < 0.6) depth = 0.6 * _DepthScale;
        #endif

        float phaseConstant = (2.0*pi*velocity[num])/wavelength[num];
        float adjustedWavelength = wavelength[num] + (2.0*WVT[num]*depth)/wavelength[num] - WVT[num];
        if (depth > 0) {
            float tempResult = (2.0*pi*dot(float3(direction[num].x, 1.0, direction[num].y), v))/adjustedWavelength;
            cosWave = cos(tempResult + phaseConstant*_Time.x);
        }

        result.x *= 0.5 * cosWave;
        result.x *= (2.0*pi*direction[num].x)/adjustedWavelength;
        result.z *= 0.5 * cosWave;
        result.z *= (2.0*pi*direction[num].y)/adjustedWavelength;

        return result;
    }

    void vert (inout appdata v, out Input o){
        UNITY_INITIALIZE_OUTPUT(Input,o);
        
        //init
        float3 v_worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        float3 tangent = float3(0,0,1);
        float3 binormal = float3(1,0,0);
        float3 normal = float3(0,0,0);

        //sampling the terrain height texture to get the ocean floor terrain height (world position)
    	float floorDepth = tex2Dlod(_OceanFloor, float4(v.texcoord,0,0)).x;
        
        float vTemp = 0.0;
    	for (int i = 0; i < num_of_wave; i++){
	    	vTemp += caculateWaveHeight(v_worldPos, i, floorDepth, steepness[i]);
            tangent.y += partialDerivative(v_worldPos, i, floorDepth).x;
            binormal.y += partialDerivative(v_worldPos, i, floorDepth).z;
        }
        v_worldPos.y = vTemp;

        normal = float3(-1.0*tangent.y, 1, -1.0*binormal.y);
        v.normal = normalize(mul((float3x3)unity_WorldToObject, normal));
        v.tangent.xyz = normalize(mul((float3x3)unity_WorldToObject, tangent));
        v.vertex.xyz = mul((float3x3)unity_WorldToObject, v_worldPos);
    }

    half4 LightingWaterSurfLight (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten){
        half NdotL = dot (s.Normal, lightDir);
        half4 c;
        c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten);
        c.a = s.Alpha;

        return c;
    }

    void surf (Input IN, inout SurfaceOutput o) {
        //pick up ocean texture as color
        fixed4 color = tex2D (_OceanTex, IN.uv_OceanTex) * _Color;

        float4 uv_SmallWaveBumpDouble = IN.uv_SmallWaveBump.xyxy + _Time.xxxx * _BumpDirection.xyzw;
        uv_SmallWaveBumpDouble *= _BumpTiling;
        float3 smallWaveBump = adjustedBump(_SmallWaveBump, uv_SmallWaveBumpDouble, _BumpStrength);
        o.Normal = normalize(smallWaveBump);
        
        //mirror reflection, pick up reflection texture and add distortion
        float4 screenUV = IN.screenPos;
        screenUV = float4(max(0.001f, screenUV.x),max(0.001f, screenUV.y),max(0.001f, screenUV.z),max(0.001f, screenUV.w));
        float3 worldNormal = WorldNormalVector(IN, float3(0,0,1));
        float2 reflDistortion = o.Normal.xz+worldNormal.xz; // * IN.viewDir.y;
        float4 reflUV = UNITY_PROJ_COORD(screenUV)+float4(reflDistortion,0,0) * _ReflDistorAmount;
        half4 reflectionColor = tex2Dproj (_ReflTex, reflUV) * _ReflAmount;
        
        //cubmap refleciton
        float3 skyColor = texCUBE (_ReflCubemap, WorldReflectionVector(IN, o.Normal)).rgb * _ReflAmount;
        reflectionColor.xyz= saturate(lerp(skyColor, reflectionColor.xyz, reflectionColor.a));
        color += reflectionColor;

		o.Albedo = color.rgb;
		o.Alpha = color.a;
        //o.Emission = texCUBE (_ReflCubemap, WorldNormalVector(IN,o.Normal)).rgb * _ReflAmount;
	}