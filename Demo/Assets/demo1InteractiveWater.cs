using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class demo1InteractiveWater : MonoBehaviour {

    //******** Mesh parameters ********
    Mesh waterMesh;
    MeshFilter waterMeshFilter;
    Material waterMat;
    public float waterWidth = 10f;
    public float gridSpacing = 0.1f;

    //******** Kernel parameters ********
    //6 is the smallest value that gives water-like motion
    int P = 8;
    float[,] storedKernelArray;

    //Update water parameters
    float updateTimer = 0f;
    int verticePerRow;
    List<Vector3> meshVertices;
    Vector3[,] height;
    Vector4[] storedData;
    Texture2D previousHeight, verticalDerivative;
    Texture2D sourceAndObstruction;
    Color Data;
    Vector4 maxScale;

    // Use this for initialization
    void Start () {
        //******** Create water mesh ********
        waterMeshFilter = GetComponent<MeshFilter>();
        GenerateWaterMesh.GenerateWater(waterMeshFilter, waterWidth, gridSpacing);
        //Need a box collider so the mouse can interact with the water
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.size = new Vector3(waterWidth, 0.1f, waterWidth);

        //******** Precompute kernel ********
        storedKernelArray = new float[2 * P + 1, 2 * P + 1];
        PrecomputeKernelValues();

        //Get water mesh and its vertice
        waterMesh = waterMeshFilter.mesh;
        meshVertices = new List<Vector3>();
        waterMesh.GetVertices(meshVertices);
        verticePerRow = (int)Mathf.Sqrt(meshVertices.Count);
        //Initialization
        height = new Vector3[verticePerRow,verticePerRow];
        storedData = new Vector4[verticePerRow * verticePerRow];
        previousHeight = new Texture2D(verticePerRow, verticePerRow);
        verticalDerivative = new Texture2D(verticePerRow, verticePerRow);
        sourceAndObstruction = new Texture2D(verticePerRow, verticePerRow);
        Data = new Color(0f, 0f, 0f, 0f);
        maxScale = new Vector4(1f,1f,1f,1f);
        for (int c = 0, i = 0; c < verticePerRow; c++)
        {
            for (int r = 0; r < verticePerRow; r++, i++)
            {
                height[c, r] = meshVertices[i];
                float temp = meshVertices[i].y;
                storedData[i] = new Vector4(temp,temp,temp,temp);

                if (c == 0 || c == verticePerRow - 1 || r == 0 || r == verticePerRow - 1)
                {
                    storedData[i].w = 0f;
                }
                else
                {
                    storedData[i].w = 1f;
                }
            }
        }

        //Set shader parameters
        waterMat = GetComponent<Renderer>().material;
        waterMat.SetFloat("_waterWidth", waterWidth);
        waterMat.SetFloat("_gridSpacing", gridSpacing);
    }
	
	// Update is called once per frame
	void Update () {
        //Move water wakes
        updateTimer += Time.deltaTime;
        if (updateTimer > 0.02f)
        {
            MoveWater();
            updateTimer = 0f;           
        }

        CreateWaterWakesWithMouse();
    }

    void MoveWater()
    {
        for (int c = 0, i = 0; c < verticePerRow; c++)
        {
            for (int r = 0; r < verticePerRow; r++, i++)
            {
                storedData[i].x = height[c, r].y;
                //height[c, r] = meshVertices[i]; 
                //this is where I should get the current computed height
                height[c, r].y += storedData[i].z;
                height[c, r].y *= storedData[i].w;
            }
        }

        Convolve();

        maxScale = FindVectorMax(storedData);
        for (int c = 0, i = 0; c < verticePerRow; c++)
        {
            for (int r = 0; r < verticePerRow; r++, i++)
            {   
                if (storedData[i].x < 0)
                {
                    Data.r = -1f * (storedData[i].x/maxScale.x);
                    Data.g = 0f;
                } else { Data.r = storedData[i].x; Data.g = 1f;}
                Data.b = maxScale.x;
                previousHeight.SetPixel(r, c, Data);

                if (storedData[i].y < 0)
                {
                    Data.r = -1f * (storedData[i].y/maxScale.y);
                    Data.g = 0f;
                }
                else { Data.r = storedData[i].y; Data.g = 1f;}
                Data.b = maxScale.y;
                verticalDerivative.SetPixel(r, c, Data);

                if (storedData[i].z < 0)
                {
                    Data.r = -1f * (storedData[i].z/maxScale.z);
                    Data.g = 0f;
                }
                else { Data.r = storedData[i].z; Data.g = 1f; }
                Data.b = maxScale.z;
                Data.a = storedData[i].w;
                sourceAndObstruction.SetPixel(r, c, Data);
                storedData[i].z = 0;
            }
        }
        previousHeight.Apply();
        verticalDerivative.Apply();
        sourceAndObstruction.Apply();
        //Transfer data to shader
        waterMat.SetTexture("_previousHeight", previousHeight as Texture);
        waterMat.SetTexture("_verticalDerivative", verticalDerivative as Texture);
        waterMat.SetTexture("_sourceAndObstruction", sourceAndObstruction as Texture);
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
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

    //G(k,l)
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

    //******** Convolve to update verticalDerivative ********
    //This might seem unnecessary, but we will save one array if we are doing it before the main loop
    void Convolve()
    {
        for (int j = 0, count = 0; j < verticePerRow; j++)
        {
            for (int i = 0; i < verticePerRow; i++, count++)
            {
                float vDeriv = 0f;

                //Will include when k, l = 0
                for (int k = -P; k <= P; k++)
                {
                    for (int l = -P; l <= P; l++)
                    {
                        //Get the precomputed values
                        //Need "+ P" because we iterate from -P and not 0, which is how they are stored in the array
                        float kernelValue = storedKernelArray[k + P, l + P];

                        //Make sure we are within the water
                        if (j + k >= 0 && j + k < verticePerRow && i + l >= 0 && i + l < verticePerRow)
                        {
                            vDeriv += kernelValue * height[j + k,i + l].y;
                        }
                        //Outside
                        else
                        {
                            //Right
                            if (j + k >= verticePerRow && i + l >= 0 && i + l < verticePerRow)
                            {
                                vDeriv += kernelValue * height[2 * verticePerRow - j - k - 1,i + l].y;
                            }
                            //Top
                            else if (i + l >= verticePerRow && j + k >= 0 && j + k < verticePerRow)
                            {
                                vDeriv += kernelValue * height[j + k,2 * verticePerRow - i - l - 1].y;
                            }
                            //Left
                            else if (j + k < 0 && i + l >= 0 && i + l < verticePerRow)
                            {
                                vDeriv += kernelValue * height[-j - k,i + l].y;
                            }
                            //Bottom
                            else if (i + l < 0 && j + k >= 0 && j + k < verticePerRow)
                            {
                                vDeriv += kernelValue * height[j + k,-i - l].y;
                            }
                        }
                    }
                }

                storedData[count].y = vDeriv;
            }
        }
    }

    //Interact with the water wakes by clicking with the mouse
    void CreateWaterWakesWithMouse()
    {
        //Fire ray from the current mouse position
        if (Input.GetKey(KeyCode.Mouse0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {

                //Convert the mouse position from global to local
                Vector3 localPos = transform.InverseTransformPoint(hit.point);

                //Loop through all the vertices of the water mesh
                for (int j = 0, count = 0; j < verticePerRow; j++)
                {
                    for (int i = 0; i < verticePerRow; i++, count++)
                    {
                        //Find the closest vertice within a certain distance from the mouse
                        float sqrDistanceToVertice = (height[j,i] - localPos).sqrMagnitude;

                        //If the vertice is within a certain range
                        float sqrDistance = 0.2f * 0.2f;
                        if (sqrDistanceToVertice < sqrDistance)
                        {
                            //Get a smaller value the greater the distance is to make it look better
                            float distanceCompensator = 1 - (sqrDistanceToVertice / sqrDistance);

                            //Add the force that now depends on how far the vertice is from the mouse
                            storedData[count].z += -0.02f * distanceCompensator;
                        }
                    }
                }
            }
        }
    }

    Vector4 FindVectorMax(Vector4[] vec)
    {
        float[] tempx, tempy, tempz, tempw;
        tempx = new float[vec.Length];
        tempy = new float[vec.Length];
        tempz = new float[vec.Length];
        tempw = new float[vec.Length];
        Vector4 maxVec;
        for (int i = 0; i < vec.Length; i++)
        {
            tempx[i] = Mathf.Abs(vec[i].x);
            tempy[i] = Mathf.Abs(vec[i].y);
            tempz[i] = Mathf.Abs(vec[i].z);
            tempw[i] = Mathf.Abs(vec[i].w);
        }

        float[] max = new float[4];
        max[0] = Mathf.Max(tempx);
        max[1] = Mathf.Max(tempy);
        max[2] = Mathf.Max(tempz);
        max[3] = Mathf.Max(tempw);
        for (int i = 0; i < max.Length; i++)
        {
            if ( 0 <= max[i] && max[i] <= 1) { max[i] = 1; }
        }
        maxVec = new Vector4(max[0],max[1],max[2],max[3]);
        return maxVec;
    }

}
