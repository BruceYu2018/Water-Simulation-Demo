using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour {

    public MRT ReadMRT { get { return mrts[readIndex]; } }
    public MRT WriteMRT { get { return mrts[writeIndex]; } }
    [SerializeField]
    Material particleDisplayMat;
    [SerializeField]
    Material particleUpdateMat;
    [SerializeField]
    MRT[] mrts;
    int readIndex = 0;
    int writeIndex = 1;

    Mesh plane;

    // Use this for initialization
    void Start () {

        plane = GetComponent<MeshFilter>().mesh;
        int bufSize = Mathf.CeilToInt(Mathf.Sqrt(plane.vertexCount * 1.0f));

        mrts = new MRT[2];
        for (int i = 0, n = mrts.Length; i < n; i++)
        {
            mrts[i] = new MRT(bufSize, bufSize);
        }

        ReadMRT.Render(particleUpdateMat, 0); // init
    }
	
	// Update is called once per frame
	void Update () {
        var buffers = ReadMRT.RenderTextures;

        particleUpdateMat.SetTexture("_PosTex", buffers[0]);
        particleUpdateMat.SetTexture("_VelTex", buffers[1]);
        particleUpdateMat.SetTexture("_AccTex", buffers[2]);
        

        WriteMRT.Render(particleUpdateMat, 1); // update

        Swap();

        particleDisplayMat.SetTexture("_PosTex", ReadMRT.RenderTextures[0]);
        GetComponent<MeshRenderer>().sharedMaterial = particleDisplayMat;
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
