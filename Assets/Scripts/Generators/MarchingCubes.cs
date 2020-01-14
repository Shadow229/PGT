using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Cube
{
    public Vector4[] CornerPos;
    public int[] pointNo;
    public float Size;

    public void Init()
    {
        CornerPos = new Vector4[8];
    }
}

public struct Triangle
{
    //public Vector3[] point;
#pragma warning disable 649 // disable unassigned variable warning
    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

   // public Vector3 normal;

    public void Init()
    {
        //point = new Vector3[3];
    }
    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }
};

public class MarchingCubes : MonoBehaviour
{

    //Examine a cell
    public static void UpdateTerrain(Vector3 CubePosition)
    {
        //index for triange calc
        int index = 0;

        //grab the terrain generator for additional values
        TerrainGeneration tG = FindObjectOfType<TerrainGeneration>();
        //grab the noise generator
        NoiseGenerator nG = FindObjectOfType<NoiseGenerator>();
        
        //new cube
        Cube cube = new Cube();
        //init the arrays
        cube.Init();
        //size of the cube
        cube.Size = tG.ChunkSize / (float)tG.VoxelsPerAxis;

        //corner bit poattern positions (clockwise - bottom then top)
        byte[] b = { 0, 4, 5, 1, 2, 6, 7, 3 };
        //world positions of all corners
        for (int x = 0; x < 8; x++)
        {
            //return the bit val of first 3 bits in byte to cover all indicies
            Vector3 ind = new Vector3(BitVal(b[x], 3), BitVal(b[x], 2), BitVal(b[x], 1));

            /*
             (0, 0, 0)
             (X, 0, 0)
             (X, 0, X)
             (0, 0, X)
             (0, X, 0)
             (X, X, 0)
             (X, X, X)
             (0, X, X)
           */

            cube.CornerPos[x] = IndexFromCube(ind.x, ind.y, ind.z);
        }

        tG.cubeGizmo = cube;

        //calculte the density value at each corner voxel
        //generate density values for w in the cube vector
        nG.GenerateDensityValue(cube);

        //Classify each vertex as inside or outside of the iso value
          for (int i = 0; i < 8; i++)
        {
            //bit OR to add values when under the isoSurface
            if (cube.CornerPos[i].w < tG.ISOValue) index |= (int)(Mathf.Pow(2f, i));
        }

        /*
         // Depreciated for code below this - however this makes it easier to understand and is how its shown in Paul Bourke's explaination (http://paulbourke.net/geometry/polygonise/) - so left in for reference

                //determine what verts are inside our isoValue
                Vector3[] vertList = new Vector3[12];

                float ISO = tG.ISOValue;

                // Cube is entirely in/out of the surface - skip the rest
                if (Lookup.EdgeTable[index] != 0)
                {
                    // Find the vertices where the surface intersects the cube 
                    if ((Lookup.EdgeTable[index] & 1) != 0)     { vertList[0] = VertexInterp(ISO, cube.CornerPos[0], cube.CornerPos[1]); } //B
                    if ((Lookup.EdgeTable[index] & 2) != 0)     { vertList[1] = VertexInterp(ISO, cube.CornerPos[1], cube.CornerPos[2]); }
                    if ((Lookup.EdgeTable[index] & 4) != 0)     { vertList[2] = VertexInterp(ISO, cube.CornerPos[2], cube.CornerPos[3]); } //A
                    if ((Lookup.EdgeTable[index] & 8) != 0)     { vertList[3] = VertexInterp(ISO, cube.CornerPos[3], cube.CornerPos[0]); }
                    if ((Lookup.EdgeTable[index] & 16) != 0)    { vertList[4] = VertexInterp(ISO, cube.CornerPos[4], cube.CornerPos[5]); } 
                    if ((Lookup.EdgeTable[index] & 32) != 0)    { vertList[5] = VertexInterp(ISO, cube.CornerPos[5], cube.CornerPos[6]); }
                    if ((Lookup.EdgeTable[index] & 64) != 0)    { vertList[6] = VertexInterp(ISO, cube.CornerPos[6], cube.CornerPos[7]); } 
                    if ((Lookup.EdgeTable[index] & 128) != 0)   { vertList[7] = VertexInterp(ISO, cube.CornerPos[7], cube.CornerPos[4]); }
                    if ((Lookup.EdgeTable[index] & 256) != 0)   { vertList[8] = VertexInterp(ISO, cube.CornerPos[0], cube.CornerPos[4]); }
                    if ((Lookup.EdgeTable[index] & 512) != 0)   { vertList[9] = VertexInterp(ISO, cube.CornerPos[1], cube.CornerPos[5]); }
                    if ((Lookup.EdgeTable[index] & 1024) != 0)  { vertList[10] = VertexInterp(ISO, cube.CornerPos[2], cube.CornerPos[6]); }
                    if ((Lookup.EdgeTable[index] & 2048) != 0)  { vertList[11] = VertexInterp(ISO, cube.CornerPos[3], cube.CornerPos[7]); }
                }


                // Create triangles for current cube configuration
                int ntriang = 0;
                for (int i = 0; Lookup.TriTable[index,i] != -1; i += 3)
                {
                    Triangle triangle = new Triangle
                    {
                        // triangle.Init();
                        a = vertList[Lookup.TriTable[index, i]],
                        b = vertList[Lookup.TriTable[index, i + 1]],
                        c = vertList[Lookup.TriTable[index, i + 2]]
                    };
                    ntriang++;

                    nG.CalculateNormal(triangle);

        */


        //!Shorthand of the above using an additional lookup table to find the correct edge table!//

        // Create triangles for current cube configuration
        for (int i = 0; Lookup.TriTable[index,i] != -1; i += 3)
        {

            // Get indices of corner points A and B for each of the three edges
            // of the cube that need to be joined to form the triangle.
            int a0 = Lookup.cornerIndexAFromEdge[Lookup.TriTable[index,i]];
            int b0 = Lookup.cornerIndexBFromEdge[Lookup.TriTable[index,i]];

            int a1 = Lookup.cornerIndexAFromEdge[Lookup.TriTable[index,i + 1]];
            int b1 = Lookup.cornerIndexBFromEdge[Lookup.TriTable[index,i + 1]];

            int a2 = Lookup.cornerIndexAFromEdge[Lookup.TriTable[index,i + 2]];
            int b2 = Lookup.cornerIndexBFromEdge[Lookup.TriTable[index,i + 2]];

            Triangle tri;
            tri.a = VertexInterp(cube.CornerPos[a0], cube.CornerPos[b0]);
            tri.b = VertexInterp(cube.CornerPos[a1], cube.CornerPos[b1]);
            tri.c = VertexInterp(cube.CornerPos[a2], cube.CornerPos[b2]);


            //add the triangle to our list
            if (tri.a != null)
            { 
                tG.tris.Add(tri);            
            }

        }

        //get a bit value from a certain byte
        float BitVal(byte _byte, int bitNumber)
        {
            bool bit = (_byte & (1 << bitNumber - 1)) != 0;
            //return the cube size if bit is true
            return bit ? cube.Size : 0;
        }


       //Linearly interpolate the position where an isosurface cuts
       //an edge between two vertices, each with their own scalar value  
        Vector3 VertexInterp(Vector4 v1, Vector4 v2)
        {
            float t = (tG.ISOValue - v1.w) / (v2.w - v1.w);
            Vector3 v1_1 = new Vector3(v1.x, v1.y, v1.z);
            Vector3 v2_1 = new Vector3(v2.x, v2.y, v2.z);

            return v1_1 + t * (v2_1 - v1_1);
        }

        //return the vertex index
        Vector4 IndexFromCube(float x, float y, float z)
        {
            Vector3 offset = new Vector3(x, y, z);
            return CubePosition + offset - Vector3.one * (cube.Size / 2f);
        }
    }


    //Function to update marching cubes triangles on the GPU
    public static void UpdateTerrainGPU(ComputeShader mcShader, ComputeBuffer voxelBuffer, ComputeBuffer triangleBuffer, int VoxelsPerAxis, float ISOValue)
    {
        int PointsPerAxis = VoxelsPerAxis + 1;
        int kernel = mcShader.FindKernel("CubeMarch");
        int MaxThreads = Mathf.CeilToInt(VoxelsPerAxis / (float)8);

        //set buffer values
        triangleBuffer.SetCounterValue(0);
        mcShader.SetBuffer(kernel, "voxelPoints", voxelBuffer);
        mcShader.SetBuffer(kernel, "triangles", triangleBuffer);
        mcShader.SetInt("voxelsPerAxis", PointsPerAxis);
        mcShader.SetFloat("ISOValue", ISOValue);

        //dispatch buffer to run
        mcShader.Dispatch(kernel, MaxThreads, MaxThreads, MaxThreads);
    }
}