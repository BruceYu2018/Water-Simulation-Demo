Shader "demoShaders/IntegratedWater" {
	Properties {
		_Color ("Ocean Color", Color) = (1,1,1,1)
		_OceanTex ("Ocean Texture", 2D) = "white" {}
		
		//Bump map
	    _SmallWaveBump ("Small Wave Bump", 2D) = "white" {}
        _BumpStrength ("Bump Strength", Float) = 1
        _BumpDirection ("Bump Direction", vector) = (1,1,1,1)
        _BumpTiling ("Bump Tiling", vector) = (1,1,1,1)

	    //shallow wave parameters
	    _OceanFloor ("Ocean Floor", 2D) = "white" {}

        //interactive wave parameters
        _iWavePosTex ("Interactive Wave Position", 2D) = "black" {}
		_iWaveNormal ("Interactive Wave Normal", 2D) = "black" {}

	    //reflaction
	    _ReflCubemap ("Reflection Cubemap", CUBE) = "" {}
	    _ReflTex ("Reflection Texture", 2D) = "white" {}
	    _ReflAmount ("Reflection Amount", Range(0.01, 1)) = 0.9
	    _ReflDistorAmount ("Refl Distortion Amount", Range(0, 0.1)) = 0.01

        //refraction
        _RefraTex ("Refraction Texture", 2D) = "white" {}
        _RefraAmount ("Refraction Amount", Range(0.01, 1)) = 0.9
        _RefraDistorAmount ("Refra Distortion Amount", Range(0, 0.1)) = 0.01
        
        //scattering
        _ScatteringKd ("Scattering Kd", Float) = 1
	    _ScatteringAtten ("Scattering Attenuation", Float) = 1
	    _ScatteringDiffuse ("Scattering Diffuse", Float) = 1
	    _depthColorScale ("depth Color Scale", Range(0, 1)) = 1

        //light
        _Shininess ("Shininess", Float) = 10.0

	}

	//DX9
	SubShader {
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
		LOD 200

		Pass{
			CGPROGRAM
	            #include "UnityCG.cginc"
	            #include "IntegratedWaterFunctionLib.cginc"

				#pragma target 3.0
	            #pragma multi_compile __ OCEAN_FLOOR_ON
	            #pragma multi_compile_fwdbase
	              
				#pragma vertex vert
		        #pragma fragment frag
			ENDCG
		}
	}
	FallBack "Diffuse"
}
