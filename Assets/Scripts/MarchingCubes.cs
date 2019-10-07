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
    public Vector3[] point;
    public Vector3 normal;

    public void Init()
    {
        point = new Vector3[3];

    }


};

public class MarchingCubes : MonoBehaviour
{

    //Examine a cell
    public static void UpdateTerrain(Chunk chunk, Vector3 CubePosition)
    {
        //arr for all voxel positions in a cube
        Vector3[] VoxelPos = new Vector3[8];

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
        cube.Size = (float)tG.ChunkSize / (float)tG.VoxelsPerAxis;

        float X = cube.Size;

        cube.CornerPos[0] = IndexFromCube(0, 0, 0);
        cube.CornerPos[1] = IndexFromCube(X, 0, 0);
        cube.CornerPos[2] = IndexFromCube(X, 0, X);
        cube.CornerPos[3] = IndexFromCube(0, 0, X);
        cube.CornerPos[4] = IndexFromCube(0, X, 0);
        cube.CornerPos[5] = IndexFromCube(X, X, 0);
        cube.CornerPos[6] = IndexFromCube(X, X, X);
        cube.CornerPos[7] = IndexFromCube(0, X, X);


        tG.cubeGizmo = cube;

        //calculte the density value at each voxel
        //generate density values for w in the cube vector
        nG.GenerateDensityValue(cube);

        //Classify each vertex as inside or outside of the iso value
        for (int i = 0; i < 8; i++)
        {
            //bit OR to add values when under the isoSurface
            //if (cube.CornerPos[0].w < tG.ISOValue) index |= 1;
            if (cube.CornerPos[i].w < tG.ISOValue) index |= (int)(Mathf.Pow(2f, i));

            //store noise val min.max
            tG.ISOValCheck(cube.CornerPos[i].w);
        }

        //index = 0;
        //if (cube.CornerPos[0].w < tG.ISOValue) index |= 1;
        //if (cube.CornerPos[1].w < tG.ISOValue) index |= 2;
        //if (cube.CornerPos[2].w < tG.ISOValue) index |= 4;
        //if (cube.CornerPos[3].w < tG.ISOValue) index |= 8;
        //if (cube.CornerPos[4].w < tG.ISOValue) index |= 16;
        //if (cube.CornerPos[5].w < tG.ISOValue) index |= 32;
        //if (cube.CornerPos[6].w < tG.ISOValue) index |= 64;
        //if (cube.CornerPos[7].w < tG.ISOValue) index |= 128;

        //determine what verts are inside our isoValue
        Vector3[] vertList = new Vector3[12];

        float ISO = tG.ISOValue;

        // Cube is entirely in/out of the surface - skip the rest
        if (Lookup.EdgeTable[index] != 0)
        {
            /* Find the vertices where the surface intersects the cube */
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
            Triangle triangle = new Triangle();
            triangle.Init();
            triangle.point[0] = vertList[Lookup.TriTable[index,i]];
            triangle.point[1] = vertList[Lookup.TriTable[index, i + 1]];
            triangle.point[2] = vertList[Lookup.TriTable[index, i + 2]];
            ntriang++;

            nG.CalculateNormal(triangle);

            //add the triangle to our list
            if (triangle.point[0] != null)
            { 
                tG.tris.Add(triangle);            
            }

        }

        /*
       Linearly interpolate the position where an isosurface cuts
       an edge between two vertices, each with their own scalar value
    */
        Vector3 VertexInterp(float ISOLevel, Vector4 v1, Vector4 v2)
        {
            float t = (ISOLevel - v1.w) / (v2.w - v1.w);
            Vector3 v1_1 = new Vector3(v1.x, v1.y, v1.z);
            Vector3 v2_1 = new Vector3(v2.x, v2.y, v2.z);

            return v1_1 + t * (v2_1 - v1_1);
        }

        Vector4 IndexFromCube(float x, float y, float z)
        {
            Vector3 offset = new Vector3(x, y, z);
            return CubePosition + offset + Vector3.one * -(((float)cube.Size) / 2f);
        }


    }
}