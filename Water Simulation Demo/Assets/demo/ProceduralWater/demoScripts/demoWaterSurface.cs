using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

public class demoWaterSurface : MonoBehaviour {

    private Renderer surfaceRender;

    //wave parameters
    public proceduralWave[] waves = new proceduralWave[4];
    private Vector4[] direction;
    private float[] depth;
    private float[] amplitude;
    private float[] velocity;
    private float[] wavelength;
    private float[] WVT;
    private float[] steepness;
    private float[] SVT;
    private RenderTexture oceanFloor;
    
    //generate water mesh
    private Mesh waterMesh;
    private MeshFilter waterMeshFilter;
    public float waterWidth = 100f;
    public float gridSpacing = 2f;

    //reflection
    public Cubemap reflCubemap;

    //utility
    public bool enableOceanFloor;
    public bool enableTess;

    void Start () {
        surfaceRender = this.GetComponent<Renderer>();

        //initialization wave parameters
        direction = new Vector4[waves.Length];
        depth = new float[waves.Length];
        amplitude = new float[waves.Length];
        velocity = new float[waves.Length];
        wavelength = new float[waves.Length];
        WVT = new float[waves.Length];
        steepness = new float[waves.Length];
        SVT = new float[waves.Length];

        //genereate water mesh and save
        waterMeshFilter = GetComponent<MeshFilter>();
        GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);
        waterMesh = waterMeshFilter.mesh;
        AssetDatabase.CreateAsset(waterMesh, "Assets/demo/ProceduralWater/demoMeshes/" + "demoOceanPlane" + ".asset");

        //tesselation and oceanfloor setup
        surfaceRender.sharedMaterial.DisableKeyword("OCEAN_FLOOR_ON");
        surfaceRender.sharedMaterial.DisableKeyword("TESS_ON");
        if (enableTess) surfaceRender.sharedMaterial.EnableKeyword("TESS_ON");
        if (enableOceanFloor)
        {
            List<Vector3> meshVertices = new List<Vector3>();
            waterMesh.GetVertices(meshVertices);
            float[] terrainHeight = new float[meshVertices.Count];
            for (int i = 0; i < meshVertices.Count; i++)
            {
                Vector3 world_v = transform.localToWorldMatrix.MultiplyPoint3x4(meshVertices[i]);
                terrainHeight[i] = Terrain.activeTerrain.SampleHeight(world_v);
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
                    tempTexture.SetPixel(x, y, new Color(terrainHeight[i],0,0,0));
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
        CubeCam.transform.position = new Vector3(0,10,0);
        CubeCam.transform.rotation = Quaternion.identity;
        CubeCam.GetComponent<Camera>().RenderToCubemap(reflCubemap);
        DestroyImmediate(CubeCam);

    }
	
	void Update () {
        if (waves.Length == 0)
            return;

        for (int i = 0; i < waves.Length; i++)
        {
            direction[i] = ConvertAngleToDirection(waves[i].direction);
            depth[i] = waves[i].depth;
            amplitude[i] = waves[i].amplitude;
            velocity[i] = waves[i].velocity;
            wavelength[i] = waves[i].wavelength;
            WVT[i] = waves[i].WVT;
            steepness[i] = waves[i].steepness;
            SVT[i] = waves[i].SVT;
        }

        surfaceRender.sharedMaterial.SetVectorArray("direction", direction);
        surfaceRender.sharedMaterial.SetFloatArray("waterDepth", depth);
        surfaceRender.sharedMaterial.SetFloatArray("amplitude", amplitude);
        surfaceRender.sharedMaterial.SetFloatArray("velocity", velocity);
        surfaceRender.sharedMaterial.SetFloatArray("wavelength", wavelength);
        surfaceRender.sharedMaterial.SetFloatArray("WVT", WVT);
        surfaceRender.sharedMaterial.SetFloatArray("steepness", steepness);
        surfaceRender.sharedMaterial.SetFloatArray("SVT", SVT);
        surfaceRender.sharedMaterial.SetInt("num_of_wave", waves.Length);

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

    //Color[] scaleToNorm(float[] values)
    //{
    //    Color[] data = new Color[values.Length];
    //    float[] temp = new float[values.Length];

    //    for (int i = 0; i < values.Length; i++)
    //    {
    //        if (values[i] < 0) data[i].g = -1f;
    //        else data[i].g = 1f;

    //        temp[i] = Mathf.Abs(values[i]);
    //    }
    //    float maximum = Mathf.Max(temp);
    //    if (maximum < 1) maximum = 1f;

    //    for (int i = 0; i < values.Length; i++)
    //    {
    //        data[i].r = temp[i] / maximum;
    //        data[i].b = maximum;
    //    }

    //    return data;
    //}
}

[System.Serializable]
public class proceduralWave
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

    public proceduralWave()
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