using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveWave : MonoBehaviour
{
    private Renderer surfaceRender;

    //Kernel parameters
    //6 is the smallest value that gives water-like motion
    int P = 8;
    float[,] storedKernelArray;

    //Update wave parameters
    float updateTimer = 0f;
    int verticePerRow;
    List<Vector3> meshVertices;
    float[,] source, obstruction;
    RenderTexture data;
    RenderTexture kernel;

    //Multiple Render Targets
    [SerializeField]
    Material waveUpdateMat;
    [SerializeField]
    MRT[] mrts;
    int readIndex = 0;
    int writeIndex = 1;
    public MRT ReadMRT { get { return mrts[readIndex]; } }
    public MRT WriteMRT { get { return mrts[writeIndex]; } }

    //Gameobject for interaction
    public GameObject InteractionObject;
    bool isInteracted = false;
    public float waveEffect = -0.01f;
    public float waveSize = 4;

    // Use this for initialization
    void Start()
    {
        surfaceRender = this.GetComponent<Renderer>();

        //******** Precompute kernel ********
        storedKernelArray = new float[2 * P + 1, 2 * P + 1];
        PrecomputeKernelValues();

        //Get water mesh and its vertice
        meshVertices = new List<Vector3>();
        GetComponent<MeshFilter>().mesh.GetVertices(meshVertices);
        verticePerRow = (int)Mathf.Sqrt(meshVertices.Count);
        //Initialization
        source = new float[verticePerRow, verticePerRow];
        obstruction = new float[verticePerRow, verticePerRow];
        data = new RenderTexture(verticePerRow, verticePerRow, 24);
        data.format = RenderTextureFormat.ARGBFloat;
        kernel = new RenderTexture(2 * P + 1, 2 * P + 1, 24);
        kernel.format = RenderTextureFormat.ARGBFloat;

        for (int c = 0, i = 0; c < verticePerRow; c++){
            for (int r = 0; r < verticePerRow; r++, i++){
                if (c == 0 || c == verticePerRow - 1 || r == 0 || r == verticePerRow - 1){
                    obstruction[c, r] = 0f;}
                else{
                    obstruction[c, r] = 1f;}
            }
        }
        ApplyFloatsToRT(storedKernelArray, kernel);

        //Set shader parameters
        waveUpdateMat.SetTexture("_kernel", kernel);
        waveUpdateMat.SetFloat("kernelSize", P);

        //Multiple render target
        mrts = new MRT[2];
        for (int i = 0, n = mrts.Length; i < n; i++)
        {
            mrts[i] = new MRT(verticePerRow, verticePerRow);
        }
    }

    void LateUpdate()
    {
        
        updateTimer += Time.deltaTime;
        if (updateTimer > 0.02f)
        {
            MoveWater();
            updateTimer = 0f;
        }
        if (isInteracted == true)
            CreateWavesWithObject();

        //CreateWaterWakesWithMouse();
    }

    void MoveWater()
    {
        //transfer verticle derivative, source and obstruction data
        RenderTexture.active = data;
        Texture2D tempTexture = new Texture2D(data.width, data.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, data.width, data.height), 0, 0, false);
        for (int c = 0, i = 0; c < data.width; c++)
        {
            for (int r = 0; r < data.height; r++, i++)
            {
                tempTexture.SetPixel(r, c, new Color(source[c, r], obstruction[c, r], meshVertices[i].x, meshVertices[i].z));
                source[c, r] = 0f;
            }
        }
        tempTexture.Apply();
        RenderTexture.active = null;
        Graphics.Blit(tempTexture, data);

        //Transfer data to shader
        waveUpdateMat.SetTexture("_data", data);

        //Multiple render target
        var buffers = ReadMRT.RenderTextures;

        waveUpdateMat.SetTexture("_previousHeight", buffers[0]);
        waveUpdateMat.SetTexture("_currentHeight", buffers[1]);
        surfaceRender.sharedMaterial.SetTexture("_iWaveNormal", buffers[2]);
        WriteMRT.Render(waveUpdateMat); // update
        Swap();
        surfaceRender.sharedMaterial.SetTexture("_iWavePosTex", ReadMRT.RenderTextures[0]);
    }

    //******** Precompute the kernel values G(k,l) ********
    void PrecomputeKernelValues()
    {
        float G_zero = CalculateG_zero();

        //P - kernel size
        for (int k = -P; k <= P; k++)
        {
            for (int l = -P; l <= P; l++)
            {
                //Need "+ P" because we iterate from -P and not 0, which is how they are stored in the array
                storedKernelArray[k + P, l + P] = CalculateG(k, l, G_zero);
            }
        }
    }

    private float CalculateG(float k, float l, float G_zero)
    {
        float delta_q = 0.001f;
        float sigma = 1f;
        float r = Mathf.Sqrt(k * k + l * l);

        float G = 0f;
        for (int n = 1; n <= 10000; n++)
        {
            float q_n = ((float)n * delta_q);
            float q_n_square = q_n * q_n;

            G += q_n_square * Mathf.Exp(-sigma * q_n_square) * BesselFunction(q_n * r);
        }
        G /= G_zero;

        return G;
    }

    //G_zero
    private float CalculateG_zero()
    {
        float delta_q = 0.001f;
        float sigma = 1f;

        float G_zero = 0f;
        for (int n = 1; n <= 10000; n++)
        {
            float q_n_square = ((float)n * delta_q) * ((float)n * delta_q);

            G_zero += q_n_square * Mathf.Exp(-sigma * q_n_square);
        }

        return G_zero;
    }

    private float BesselFunction(float x)
    {
        //From http://people.math.sfu.ca/~cbm/aands/frameindex.htm
        //page 369 - 370

        float J_zero_of_X = 0f;

        //Test to see if the bessel functions are valid
        //Has to be above -3
        if (x <= -3f)
        {
            Debug.Log("smaller");
        }

        //9.4.1
        //-3 <= x <= 3
        if (x <= 3f)
        {
            //Ignored the small rest term at the end
            J_zero_of_X =
                1f -
                    2.2499997f * Mathf.Pow(x / 3f, 2f) +
                    1.2656208f * Mathf.Pow(x / 3f, 4f) -
                    0.3163866f * Mathf.Pow(x / 3f, 6f) +
                    0.0444479f * Mathf.Pow(x / 3f, 8f) -
                    0.0039444f * Mathf.Pow(x / 3f, 10f) +
                    0.0002100f * Mathf.Pow(x / 3f, 12f);
        }

        //9.4.3
        //3 <= x <= infinity
        else
        {
            //Ignored the small rest term at the end
            float f_zero =
                0.79788456f -
                    0.00000077f * Mathf.Pow(3f / x, 1f) -
                    0.00552740f * Mathf.Pow(3f / x, 2f) -
                    0.00009512f * Mathf.Pow(3f / x, 3f) -
                    0.00137237f * Mathf.Pow(3f / x, 4f) -
                    0.00072805f * Mathf.Pow(3f / x, 5f) +
                    0.00014476f * Mathf.Pow(3f / x, 6f);

            //Ignored the small rest term at the end
            float theta_zero =
                x -
                    0.78539816f -
                    0.04166397f * Mathf.Pow(3f / x, 1f) -
                    0.00003954f * Mathf.Pow(3f / x, 2f) -
                    0.00262573f * Mathf.Pow(3f / x, 3f) -
                    0.00054125f * Mathf.Pow(3f / x, 4f) -
                    0.00029333f * Mathf.Pow(3f / x, 5f) +
                    0.00013558f * Mathf.Pow(3f / x, 6f);

            //Should be cos and not acos
            J_zero_of_X = Mathf.Pow(x, -1f / 3f) * f_zero * Mathf.Cos(theta_zero);
        }
        return J_zero_of_X;
    }

    //Interact with the water wakes by clicking with the mouse
    void CreateWaterWakesWithMouse()
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                Vector3 localPos = transform.InverseTransformPoint(hit.point);
                for (int j = 0, count = 0; j < verticePerRow; j++){
                    for (int i = 0; i < verticePerRow; i++, count++){
                        float sqrDistanceToVertice = (meshVertices[count] - localPos).sqrMagnitude;

                        //If the vertice is within a certain range
                        float sqrDistance = waveSize;
                        if (sqrDistanceToVertice < sqrDistance)
                        {
                            float distanceCompensator = 1 - (sqrDistanceToVertice / sqrDistance);
                            source[j, i] += waveEffect * distanceCompensator;
                        }
                    }
                }
            }
        }
    }

    void CreateWavesWithObject()
    {
        Vector3 localPos = transform.InverseTransformPoint(InteractionObject.transform.position);
        for (int j = 0, count = 0; j < verticePerRow; j++)
        {
            for (int i = 0; i < verticePerRow; i++, count++)
            {
                float sqrtDistanceToVertice = (meshVertices[count] - localPos).sqrMagnitude;
                float thresholdDist = waveSize;
                if (sqrtDistanceToVertice < thresholdDist)
                {
                    float distCompensator = 1 - (sqrtDistanceToVertice / thresholdDist);
                    source[j, i] += waveEffect * distCompensator;
                }
            }
        }
    }

    void ApplyFloatsToRT(float[,] floats, RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;
        Texture2D tempTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBAFloat, false);
        tempTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
        for (int i = 0; i < tempTexture.width; i++)
        {
            for (int j = 0; j < tempTexture.height; j++)
            {
                tempTexture.SetPixel(j, i, new Color(floats[i, j], 0, 0, 0));
            }
        }
        tempTexture.Apply();
        RenderTexture.active = null;
        Graphics.Blit(tempTexture, renderTexture);
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

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name.CompareTo(InteractionObject.name) == 0)
        {
            isInteracted = true;
        }
    }

    void onTriggerExit(Collider collider)
    {
        if (collider.gameObject.name.CompareTo(InteractionObject.name) == 0)
        {
            isInteracted = false;
        }
    }
}
