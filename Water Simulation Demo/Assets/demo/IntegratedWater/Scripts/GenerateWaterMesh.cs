using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateWaterMesh
{
    public static List<Vector3[]> vertices2dArray = new List<Vector3[]>();
    public static void GenerateWater(MeshFilter waterMeshFilter, float size, float spacing)
    {
        //Determine the number of vertices per row/column (is always a square)
        int verticesPerRow = (int)Mathf.Round(size / spacing) + 1;
        Vector2[] uv = new Vector2[verticesPerRow * verticesPerRow];

        for (int i=0, z = 0; z < verticesPerRow; z++)
        {
            vertices2dArray.Add(new Vector3[verticesPerRow]);
            for (int x = 0; x < verticesPerRow; x++,i++)
            {
                Vector3 current_point = new Vector3();
                current_point.x = x * spacing-(size/2);
                current_point.z = z * spacing-(size/2);
                current_point.y = 0;

                vertices2dArray[z][x] = current_point;
                uv[i] = new Vector2((x/size)*spacing, (z/size)*spacing);
            }
        }

        //Unfold the 2d array of verticies into a 1d array.
        Vector3[] unfolded_verts = new Vector3[verticesPerRow * verticesPerRow];
        for (int i = 0; i < vertices2dArray.Count; i++)
        {
            vertices2dArray[i].CopyTo(unfolded_verts, i * verticesPerRow);
        }
        //Create triangles
        int[] triangles = new int[(verticesPerRow-1) * (verticesPerRow - 1) * 6];
        for (int ti = 0, vi = 0, y = 0; y < verticesPerRow - 1; y++, vi++)
        {
            for (int x = 0; x < verticesPerRow - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + verticesPerRow;
                triangles[ti + 5] = vi + verticesPerRow + 1;
            }
        }

        //Generate the mesh
        Mesh waterMesh = new Mesh();
        waterMesh.vertices = unfolded_verts;
        waterMesh.uv = uv;
        waterMesh.triangles = triangles;
        waterMesh.RecalculateBounds();
        waterMesh.RecalculateNormals();
        waterMesh.name = "demoOceanPlane";

        //Add the generated mesh to the GameObject
        waterMeshFilter.mesh.Clear();
        waterMeshFilter.mesh = waterMesh;
    }

}