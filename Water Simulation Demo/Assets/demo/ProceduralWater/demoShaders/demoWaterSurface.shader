Shader "demoShaders/demoWaterSurface" {
	Properties {
		_Color ("Ocean Color", Color) = (1,1,1,1)
		_OceanTex ("Ocean Texture", 2D) = "white" {}
		
		//Bump map
	    _SmallWaveBump ("Small Wave Bump", 2D) = "white" {}
        _BumpStrength ("Bump Strength", Float) = 1
        _BumpDirection ("Bump Direction", vector) = (1,1,1,1)
        _BumpTiling ("Bump Tiling", vector) = (1,1,1,1)

	    //wave parameters
	    _OceanFloor ("Ocean Floor", 2D) = "white" {}
	    _DepthScale ("Depth Scale", Float) = 1.0

	    //reflaction
	    _ReflCubemap ("Reflection Cubemap", CUBE) = ""{}
	    _ReflTex ("Reflection Texture", 2D) = "white" {}
	    _ReflAmount ("Reflection Amount", Range(0.01, 1)) = 0.5
	    _ReflDistorAmount ("Refl Distortion Amount", Float) = 1.0

	    //tessellation setup
		_Tess ("Tessellation", Float) = 4.0
	    _minDist ("TessMin", Range(-180.0, 0.0)) = 10.0
	    _maxDist ("TessMax", Range(20.0, 500.0)) = 25.0

	}

	//DX11
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		    #include "UnityCG.cginc"
		    #include "demoFunctionLibrary.cginc"

			#pragma target 5.0
            #pragma multi_compile __ TESS_ON
            #pragma multi_compile __ OCEAN_FLOOR_ON
              
			#pragma surface surf WaterSurfLight addshadow vertex:vert //tessellate:tessDistance nolightmap
			//#pragma surface surf Lambert addshadow vertex:vert
		ENDCG
	}

	//DX9
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		    #include "UnityCG.cginc"
		    #include "demoFunctionLibrary.cginc"

			#pragma target 3.0
            #pragma multi_compile __ OCEAN_FLOOR_ON

			#pragma surface surf WaterSurfLight addshadow vertex:vert
			//#pragma surface surf Lambert addshadow vertex:vert
		ENDCG
	}
	FallBack "Diffuse"
}
