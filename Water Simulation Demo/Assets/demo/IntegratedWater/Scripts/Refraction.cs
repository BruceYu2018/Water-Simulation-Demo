using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Refraction : MonoBehaviour {

    public Camera RefracCam;
    public Camera viewCamera;
    Material RefracMat;
    RenderTexture RefracTex;

    void Start()
    {
        if (null == RefracCam)
        { 
            GameObject go = new GameObject();
            go.name = "refracCamera";
            RefracCam = go.AddComponent<Camera>();
            RefracCam.CopyFrom(viewCamera);

            //RefracCam.fieldOfView *= 1.1f;
            RefracCam.enabled = false;
            RefracCam.cullingMask = ~(1 << LayerMask.NameToLayer("Water"));
        }

        if (null == RefracMat)
        {
            RefracMat = this.GetComponent<Renderer>().sharedMaterial;
        }

        RefracTex = new RenderTexture(Mathf.FloorToInt(RefracCam.pixelWidth),
        Mathf.FloorToInt(RefracCam.pixelHeight), 24);
        RefracTex.hideFlags = HideFlags.DontSave;
        RefracCam.targetTexture = RefracTex;

        Matrix4x4 P = GL.GetGPUProjectionMatrix(RefracCam.projectionMatrix, false);
        Matrix4x4 V= RefracCam.worldToCameraMatrix;
        RefracMat.SetMatrix("_RefracCameraVP", P*V);
    }

    public void Update()
    {
        RefracCam.transform.position = viewCamera.transform.position;
        RefracCam.transform.rotation = viewCamera.transform.rotation;

        RefracCam.targetTexture = RefracTex;
        RefracCam.Render();

        RefracMat.SetTexture("_RefraTex", RefracCam.targetTexture);

    }
}
