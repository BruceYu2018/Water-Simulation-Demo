using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public Material mat;
	// Use this for initialization
	void Start () {
        var materialProperty = new MaterialPropertyBlock();
        float[] floatArray = new float[] { 1f, 2f };
        //float[][] floatArray = new float[2][];
        //floatArray[0] = new float[2] { 1, 1 };
        //floatArray[1] = new float[2] { 2, 2 };

        materialProperty.SetFloatArray("arrayName", floatArray);
        gameObject.GetComponent<Renderer>().SetPropertyBlock(materialProperty);
        //mat.SetFloatArray("arrayName", floatArray);

        //Shader.SetGlobalFloatArray("arrayName", floatArray);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
