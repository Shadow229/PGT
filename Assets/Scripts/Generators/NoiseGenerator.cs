using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{
    public ComputeShader DensityShader;

    [Header("Noise Values")]
    // Noise settings
    public Vector3 Offset;
    [Range(1,8)]
    public int Octaves;

    public float NoiseScale = 2.5f;
    public float NoiseWeight = 2f;

    public float FloorOffset;
    public float WeightMultiplier;
    public float HardFloor;
    public float HardFloorWeight;

    [Range(1f, 4f)]
    public float Lacunarity = 2f;

    [Range(0f, 1f)]
    public float Persistence = 1f;

    [HideInInspector]
    public bool settingsUpdated = false;
    [HideInInspector]
    public System.Random rnd;
    [HideInInspector]
    public float RndSeeded = 0f;
    [HideInInspector]
    public int Seed = 0;


    //density value for a single point if style is voxelated
    public float GenerateDensityValue(Vector3 pos)
    {
        rnd = new System.Random(Seed);
        RndSeeded = (float)rnd.NextDouble();
        float c = FBM(pos);

        return c;
    }


    //density values for a cube at all given points
    public void GenerateDensityValue(Cube cube)
    {
        rnd = new System.Random(Seed);
        RndSeeded = (float)rnd.NextDouble();

        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = cube.CornerPos[i];

            float c = FBM(pos);

            cube.CornerPos[i].w = c;
        }
    }

    //fractal brownian motion to create more realistic terrain
    public float FBM(Vector3 vec)
    {
        float Frequency = NoiseScale / 100f;
        float Amplitude = 1;
        float weight = 1f;
        float noise = 0;
       // float offsetRange = 1000;

        for (int i = 0; i < Octaves; i++)
        {
            //Passing in a seeded offset at octave level causes mesh issues not seen on the GPU - removed from both functions to match terrains up
            ///Vector3 OctaveOffset = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1) * 1000;

            float f = PerlinNoise.CNoise(vec * Frequency + Offset);
            float v = 1 - Mathf.Abs(f);
            v *= v;
            v *= weight;
            weight = Mathf.Max(Mathf.Min(v * WeightMultiplier, 1), 0);
            noise += v * Amplitude;
            Amplitude *= Persistence;
            Frequency *= Lacunarity;
        }

        float c = -(vec.y + FloorOffset) + (noise * NoiseWeight);

        if (vec.y < HardFloor)
        {
            c += HardFloorWeight;
        }

        return c;
    }

    //function to reverse normals on a mesh
    public void ReverseNormals(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)

            normals[i] = -normals[i];
        mesh.normals = normals;

        for (int m = 0; m < mesh.subMeshCount; m++)
        {
            int[] triangles = mesh.GetTriangles(m);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int temp = triangles[i + 0];
                triangles[i + 0] = triangles[i + 1];
                triangles[i + 1] = temp;
            }

            mesh.SetTriangles(triangles, m);
        }
    }
    

    //generate funtion for GPU calculated noise from the compute shader
    public ComputeBuffer Generate(ComputeBuffer voxelBuffer, int voxelsPerAxis, float chunkSize, Vector3 worldSize, Vector3 chunkCentre, float voxelSpacing, NoiseType NoiseType)
    {
        //find the compute shader kernel
        int kernel = DensityShader.FindKernel("NoiseDensity");

        //calc seeded random value
        rnd = new System.Random(Seed);

        //required variables
        int PointsPerAxis = voxelsPerAxis + 1;
        var OctaveOffsets = new Vector3[Octaves];

        //get random offsets through number of octaves
        for (int i = 0; i < Octaves; i++)
        {
            //random between -1 and 1 * 1000 for reasonable offset value
            OctaveOffsets[i] = new Vector3((float)rnd.NextDouble() *2 - 1, (float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1) * 1000;
        }

        //set random offsets to a buffer to be used by the noise shader
        ComputeBuffer OctaveOffsetsBuffer = new ComputeBuffer(OctaveOffsets.Length, sizeof(float) * 3);
        OctaveOffsetsBuffer.SetData(OctaveOffsets);

        //return value
        DensityShader.SetBuffer(0, "voxelPoints", voxelBuffer);
        DensityShader.SetBuffer(0, "octaveOffsets", OctaveOffsetsBuffer);

        //set all compute shader values
        DensityShader.SetInt("PointsPerAxis", PointsPerAxis);
        DensityShader.SetVector("chunkCentre", new Vector4(chunkCentre.x, chunkCentre.y, chunkCentre.z));
        DensityShader.SetFloat("voxelSpacing", voxelSpacing);
        DensityShader.SetFloat("chunkSize", chunkSize);
        DensityShader.SetVector("worldSize", worldSize);
        DensityShader.SetInt("noiseType", (int)NoiseType);

        //set all remaining Noise Variables
        DensityShader.SetFloat("noiseScale", NoiseScale);
        DensityShader.SetFloat("noiseWeight", NoiseWeight);
        DensityShader.SetVector("offset", Offset);

        DensityShader.SetFloat("floorOffset", FloorOffset);
        DensityShader.SetFloat("weightMultiplier", WeightMultiplier);
        DensityShader.SetFloat("hardFloor", HardFloor);
        DensityShader.SetFloat("hardFloorWeight", HardFloorWeight);

        DensityShader.SetInt("octaves", Mathf.Max(1, Octaves));
        DensityShader.SetFloat("lacunarity", Lacunarity);
        DensityShader.SetFloat("persistence", Persistence);

        int MaxThreads = Mathf.CeilToInt(PointsPerAxis / (float)8);

        //dispatch the shader to run
        DensityShader.Dispatch(kernel, MaxThreads, MaxThreads, MaxThreads);


        //immediatly release the offsets buffer
        OctaveOffsetsBuffer.Release();

        return voxelBuffer;

    }


    void OnValidate()
    {
        settingsUpdated = true;
    }
}