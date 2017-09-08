using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPH : MonoBehaviour {

    public GameObject System { get { return system; } }
    public MRT ReadMRT { get { return mrts[readIndex]; } }
    public MRT WriteMRT { get { return mrts[writeIndex]; } }

    [SerializeField]
    int vertexCount = 900;
    [SerializeField]
    Material SPH_DisplayMat;
    [SerializeField]
    Material SPH_UpdateMat;
    [SerializeField]
    Material SPH_UpdateDensityMat;

    [SerializeField]
    MRT[] mrts;
    int readIndex = 0;
    int writeIndex = 1;

    GameObject system;

    const int VERTEXLIMIT = 10000;
    RenderTexture initialPos;

    GameObject Build(int vertCount, out int bufSize, out Vector3[] verts)
    {
        System.Type[] objectType = new System.Type[2];
        objectType[0] = typeof(MeshFilter);
        objectType[1] = typeof(MeshRenderer);

        GameObject go = new GameObject("ParticleMesh", objectType);

        Mesh particleMesh = new Mesh();
        particleMesh.name = vertCount.ToString();

        int vc = Mathf.Min(VERTEXLIMIT, vertCount);
        bufSize = Mathf.CeilToInt(Mathf.Sqrt(vertCount * 1.0f));

        verts = new Vector3[vc];
        Vector2[] texcoords = new Vector2[vc];
        int[] indices = new int[vc];

        for (int height = 0, i = 0; height < 9; height++)
        {
            for (int length = 0; length < 10; length++)
            {
                for (int width = 0; width < 10; width++, i++)
                {
                    float k = i;

                    float tx = 1f * (k % bufSize+0.5f) / bufSize;
                    float ty = 1f * ((int)(k / bufSize)+0.5f) / bufSize;

                    verts[i] = new Vector3(width, height, length);
                    texcoords[i] = new Vector2(tx, ty);
                    indices[i] = i;
                }
            }
        }

        particleMesh.vertices = verts;
        particleMesh.uv = texcoords;
        particleMesh.uv2 = texcoords;

        particleMesh.SetIndices(indices, MeshTopology.Points, 0);
        particleMesh.RecalculateBounds();

        go.GetComponent<MeshRenderer>().material = SPH_DisplayMat;
        go.GetComponent<MeshFilter>().sharedMesh = particleMesh;

        return go;
    }

    void Start () {
        int bufSize;
        Vector3[] meshVertices;
        system = Build(vertexCount, out bufSize, out meshVertices);
        system.transform.parent = transform;

        mrts = new MRT[3];
        for (int i = 0, n = mrts.Length; i < n; i++)
        {
            mrts[i] = new MRT(bufSize, bufSize);
        }

        // create a render texture and transfer the initial particle postions to the update shader
        initialPos = new RenderTexture(bufSize, bufSize, 24);
        initialPos.format = RenderTextureFormat.ARGBFloat;
        RenderTexture.active = initialPos;
        Texture2D tempTexture = new Texture2D(initialPos.width, initialPos.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, initialPos.width, initialPos.height), 0, 0, false);
        for (int c = 0, i = 0; c < initialPos.width; c++)
        {
            for (int r = 0; r < initialPos.height; r++, i++)
            {
                tempTexture.SetPixel(r, c, new Color(meshVertices[i].x, meshVertices[i].y, meshVertices[i].z, 1));
                //Debug.Log(meshVertices[i].x.ToString()+" "+meshVertices[i].y.ToString()+" "+meshVertices[i].z.ToString() );
            }
        }
        tempTexture.Apply();
        RenderTexture.active = null;
        Graphics.Blit(tempTexture, initialPos);

        SPH_UpdateMat.SetTexture("_PosTex", initialPos);
        SPH_UpdateDensityMat.SetTexture("_PosTex", initialPos);
        mrts[2].Render(SPH_UpdateDensityMat);
        var pressureBuffers = mrts[2].RenderTextures;
        SPH_UpdateMat.SetTexture("_DensityTex", pressureBuffers[0]);
        SPH_UpdateMat.SetTexture("_PressureTex", pressureBuffers[1]);
        ReadMRT.Render(SPH_UpdateMat);

        var buffers = ReadMRT.RenderTextures;
        RenderTexture.active = pressureBuffers[0];
        tempTexture = new Texture2D(buffers[1].width, buffers[1].height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, buffers[1].width, buffers[1].height), 0, 0, false);
        for (int i = 0; i < tempTexture.width; i++)
            for (int j = 0; j < tempTexture.height; j++)
            {
                //Debug.Log(tempTexture.GetPixel(j, i).r.ToString() + " " + tempTexture.GetPixel(j, i).g.ToString() + " " + tempTexture.GetPixel(j, i).b.ToString());
            }
        RenderTexture.active = null;

    }

    void Update () {
        var buffers = ReadMRT.RenderTextures;
        var pressureBuffers = mrts[2].RenderTextures;

        //RenderTexture.active = buffers[0];
        //Texture2D tempTexture = new Texture2D(buffers[0].width, buffers[0].height, TextureFormat.RGBAFloat, false);
        //tempTexture.ReadPixels(new Rect(0, 0, buffers[0].width, buffers[0].height), 0, 0, false);
        //for (int i = 0; i < tempTexture.width; i++)
        //    for (int j = 0; j < tempTexture.height; j++)
        //    {
        //        Debug.Log(tempTexture.GetPixel(j, i).r.ToString() + " " + tempTexture.GetPixel(j, i).g.ToString() + " " + tempTexture.GetPixel(j, i).b.ToString());
        //    }
        //RenderTexture.active = null;

        SPH_UpdateMat.SetTexture("_PosTex", buffers[0]);
        SPH_UpdateMat.SetTexture("_VelTex", buffers[1]);
        SPH_UpdateMat.SetTexture("_AccTex", buffers[2]);
        SPH_UpdateMat.SetTexture("_DensityTex", pressureBuffers[0]);
        SPH_UpdateMat.SetTexture("_PressureTex", pressureBuffers[1]);

        WriteMRT.Render(SPH_UpdateMat); // update

        Swap();

        SPH_DisplayMat.SetTexture("_PosTex", ReadMRT.RenderTextures[0]);
        SPH_UpdateDensityMat.SetTexture("_PosTex", ReadMRT.RenderTextures[0]);
        mrts[2].Render(SPH_UpdateDensityMat);
    }

    void Swap()
    {
        var tmp = readIndex;
        readIndex = writeIndex;
        writeIndex = tmp;
    }

    void OnDestroy()
    {
        for (int i = 0, n = mrts.Length; i < n; i++)
        {
            mrts[i].Release();
        }
    }
}
