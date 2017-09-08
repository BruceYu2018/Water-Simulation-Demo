using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider))]
public class IntegratedWater : MonoBehaviour {

    private Renderer surfaceRender;

    //shallow wave parameters
    public shallowWave[] shallowWaves = new shallowWave[4];
    private Vector4[] direction_SW;
    private float[] depth_SW;
    private float[] amplitude_SW;
    private float[] velocity_SW;
    private float[] wavelength_SW;
    private float[] WVT_SW;
    private float[] steepness_SW;
    private float[] SVT_SW;
    private RenderTexture oceanFloor;

    //gerstner wave parameters
    public gerstnerWave[] gerstnerWaves = new gerstnerWave[4];
    private Vector4[] direction_GW;
    private float[] steepness_GW;
    private float[] amplitude_GW;
    private float[] velocity_GW;
    private float[] wavelength_GW;

    //generate water mesh
    private Mesh waterMesh;
    private MeshFilter waterMeshFilter;
    public float waterWidth = 100f;
    public float gridSpacing = 1f;

    //reflection
    public Cubemap reflCubemap;

    //utility
    public bool enableOceanFloor;

    void Start()
    {
        surfaceRender = this.GetComponent<Renderer>();

        //initialize shallow wave parameters
        direction_SW = new Vector4[shallowWaves.Length];
        depth_SW = new float[shallowWaves.Length];
        amplitude_SW = new float[shallowWaves.Length];
        velocity_SW = new float[shallowWaves.Length];
        wavelength_SW = new float[shallowWaves.Length];
        WVT_SW = new float[shallowWaves.Length];
        steepness_SW = new float[shallowWaves.Length];
        SVT_SW = new float[shallowWaves.Length];

        //initialize gerstner wave parameters
        direction_GW = new Vector4[gerstnerWaves.Length];
        steepness_GW = new float[gerstnerWaves.Length];
        amplitude_GW = new float[gerstnerWaves.Length];
        velocity_GW = new float[gerstnerWaves.Length];
        wavelength_GW = new float[gerstnerWaves.Length];

        //genereate water mesh and save
        waterMeshFilter = GetComponent<MeshFilter>();
        GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);
        waterMesh = waterMeshFilter.mesh;
        #if UNITY_EDITOR
            AssetDatabase.CreateAsset(waterMesh, "Assets/demo/IntegratedWater/" + "IntegratedWaterOceanPlane" + ".asset");
        #endif

        //Need a box collider so object can interact with the water
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.size = new Vector3(waterWidth, 0.1f, waterWidth);

        //oceanfloor setup
        surfaceRender.sharedMaterial.DisableKeyword("OCEAN_FLOOR_ON");
        if (enableOceanFloor)
        {
            List<Vector3> meshVertices = new List<Vector3>();
            waterMesh.GetVertices(meshVertices);
            Vector3[] terrainPosition = new Vector3[meshVertices.Count];
            for (int i = 0; i < meshVertices.Count; i++)
            {
                Vector3 world_v = transform.localToWorldMatrix.MultiplyPoint3x4(meshVertices[i]);
                terrainPosition[i].y = Terrain.activeTerrain.SampleHeight(world_v);
                terrainPosition[i].x = meshVertices[i].x;
                terrainPosition[i].z = meshVertices[i].z;
            }
            int verticePerRow = (int)Mathf.Sqrt(meshVertices.Count);

            oceanFloor = new RenderTexture(verticePerRow, verticePerRow, 24);
            oceanFloor.format = RenderTextureFormat.ARGBFloat;
            Texture2D tempTexture = new Texture2D(verticePerRow, verticePerRow, TextureFormat.RGBAFloat, false);
            RenderTexture.active = oceanFloor;
            tempTexture.ReadPixels(new Rect(0, 0, oceanFloor.width, oceanFloor.height), 0, 0, false);
            for (int y = 0, i = 0; y < verticePerRow; y++)
            {
                for (int x = 0; x < verticePerRow; x++, i++)
                {
                    tempTexture.SetPixel(x, y, new Color(terrainPosition[i].x, terrainPosition[i].y, terrainPosition[i].z, 0));
                }
            }
            tempTexture.Apply();
            RenderTexture.active = null;
            Graphics.Blit(tempTexture, oceanFloor);
            surfaceRender.sharedMaterial.SetTexture("_OceanFloor", oceanFloor);
            surfaceRender.sharedMaterial.EnableKeyword("OCEAN_FLOOR_ON");
        }

        // reflection cubemap
        GameObject CubeCam = new GameObject("CubeCam");
        CubeCam.AddComponent<Camera>();
        CubeCam.transform.position = new Vector3(0, 10, 0);
        CubeCam.transform.rotation = Quaternion.identity;
        CubeCam.GetComponent<Camera>().RenderToCubemap(reflCubemap);
        DestroyImmediate(CubeCam);

    }

    void Update()
    {
        if (shallowWaves.Length == 0 || gerstnerWaves.Length == 0)
            return;

        //shallow wave update
        for (int i = 0; i < shallowWaves.Length; i++)
        {
            direction_SW[i] = ConvertAngleToDirection(shallowWaves[i].direction);
            depth_SW[i] = shallowWaves[i].depth;
            amplitude_SW[i] = shallowWaves[i].amplitude;
            velocity_SW[i] = shallowWaves[i].velocity;
            wavelength_SW[i] = shallowWaves[i].wavelength;
            WVT_SW[i] = shallowWaves[i].WVT;
            steepness_SW[i] = shallowWaves[i].steepness;
            SVT_SW[i] = shallowWaves[i].SVT;
        }

        surfaceRender.sharedMaterial.SetInt("num_of_wave_SW", shallowWaves.Length);
        surfaceRender.sharedMaterial.SetVectorArray("direction_SW", direction_SW);
        surfaceRender.sharedMaterial.SetFloatArray("waterDepth_SW", depth_SW);
        surfaceRender.sharedMaterial.SetFloatArray("amplitude_SW", amplitude_SW);
        surfaceRender.sharedMaterial.SetFloatArray("velocity_SW", velocity_SW);
        surfaceRender.sharedMaterial.SetFloatArray("wavelength_SW", wavelength_SW);
        surfaceRender.sharedMaterial.SetFloatArray("WVT_SW", WVT_SW);
        surfaceRender.sharedMaterial.SetFloatArray("steepness_SW", steepness_SW);
        surfaceRender.sharedMaterial.SetFloatArray("SVT_SW", SVT_SW);

        //gerstner wave update
        for (int i = 0; i < gerstnerWaves.Length; i++)
        {
            direction_GW[i] = ConvertAngleToDirection(gerstnerWaves[i].direction);
            steepness_GW[i] = gerstnerWaves[i].steepness;
            amplitude_GW[i] = gerstnerWaves[i].amplitude;
            velocity_GW[i] = gerstnerWaves[i].velocity;
            wavelength_GW[i] = gerstnerWaves[i].wavelength;
        }

        surfaceRender.sharedMaterial.SetInt("num_of_wave_GW", gerstnerWaves.Length);
        surfaceRender.sharedMaterial.SetVectorArray("direction_GW", direction_GW);
        surfaceRender.sharedMaterial.SetFloatArray("steepness_GW", steepness_GW);
        surfaceRender.sharedMaterial.SetFloatArray("amplitude_GW", amplitude_GW);
        surfaceRender.sharedMaterial.SetFloatArray("velocity_GW", velocity_GW);
        surfaceRender.sharedMaterial.SetFloatArray("wavelength_GW", wavelength_GW);

    }

    Vector2 ConvertAngleToDirection(float Angle)
    {
        Vector2 direction = new Vector2(0f, 0f);
        Vector3 temp = new Vector3(0f, 0f, 0f);
        if (Angle <= 180.0f)
        {
            temp = Vector3.Slerp(Vector3.forward, -Vector3.forward, (Angle) / 180.0f);
            direction = new Vector2(temp.x, temp.z);
        }
        if (Angle > 180.0f)
        {
            temp = Vector3.Slerp(-Vector3.forward, Vector3.forward, (Angle - 180.0f) / 180.0f);
            direction = new Vector2(-temp.x, temp.z);
        }

        return direction;
    }
}

[System.Serializable]
public class shallowWave
{
    [Range(0, 360)]
    public float direction;
    public float depth;
    public float amplitude;
    public float velocity;
    public float wavelength;
    public float WVT; //Wavelength Variance Tolerance
    public float steepness;
    public float SVT; //Steepness Variance Tolerance

    public shallowWave()
    {
        direction = 90f;
        depth = 500f;
        amplitude = 3f;
        velocity = 20f;
        wavelength = 20f;
        WVT = 1f;
        steepness = 5f;
        SVT = 0.1f;
    }
}

[System.Serializable]
public class gerstnerWave
{
    [Range(0, 360)]
    public float direction;
    [Range(0, 2)]
    public float steepness;
    public float amplitude;
    public float velocity;
    public float wavelength;

    public gerstnerWave()
    {
        direction = 90f;
        steepness = 0.5f;
        amplitude = 0.1f;
        velocity = 40f;
        wavelength = 10f;
    }
}
