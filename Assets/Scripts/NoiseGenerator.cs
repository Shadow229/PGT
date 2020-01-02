using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{
    public ComputeShader DensityShader;

    public int Seed = 0;
    // Noise settings
    public Vector3 Offset;
    public int Octaves;

    public float NoiseScale = 2.5f;
    public float NoiseWeight = 2f;

    public float FloorOffset;
    public float WeightMultiplier;
    //public bool CloseEdges;
    public float HardFloor;
    public float HardFloorWeight;

    [Range(0f, 1f)]
    public float HurstExponent = 0.5f;

    [Range(1.9f, 2.1f)]
    public float Lacunarity = 2f;

    public float Persistence = 1f;

    public bool settingsUpdated = false;

    public System.Random rnd;
    public float RndSeeded = 0f;



    private void Start()
    {
        //seed our random number early!
        rnd = new System.Random(Seed);
        RndSeeded = (float)rnd.NextDouble();
    }

    public float GenerateDensityValue(Vector3 pos)
    {
        //var Rng = new System.Random(Seed);

        Vector3 SeededPos = new Vector3(pos.x * RndSeeded, pos.y * RndSeeded, pos.z * RndSeeded);

        Vector3 v = SeededPos + Offset;

        float c = FBM(v);

        return c;
    }


    public void GenerateDensityValue(Cube cube)
    {
        
        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = cube.CornerPos[i];
   
            Vector3 SeededPos = new Vector3(pos.x * RndSeeded, pos.y * RndSeeded, pos.z * RndSeeded);

            Vector3 v = SeededPos + Offset;

            float c = FBM(v);

            cube.CornerPos[i].w = c;
        }
    }


    public float FBM(Vector3 vec)
    {
        float Gain = (float)Math.Pow(2.0, -HurstExponent);
        float Frequency = NoiseScale;
        float Amplitude = Persistence;
        float weight = 1f;
        float noise = 0;

        for (int i = 0; i < Octaves; i++)
        {
            float f = Perlin3D(vec * Frequency);
            float v = 1 - Mathf.Abs(f);
            v *= v;
            v *= weight;

            weight = Mathf.Max(Mathf.Min(v * WeightMultiplier, 1), 0);
            noise += v * Amplitude;
            Amplitude *= Gain;
            Frequency *= Lacunarity;
        }

        float c = -(vec.y + FloorOffset) + noise * NoiseWeight;

        if (vec.y < HardFloor)
        {
            c += HardFloorWeight;
        }

        return c;
    }


    public float Perlin3D(Vector3 vec)
    {
        float d = .01f;

        float x, y, z;
        x = vec.x * d;
        z = vec.z * d;
        y = vec.y * d;

        //get all permutations of noise
        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        //return the average of all noise permutations
        float ABC = AB + BC + AC + BA + CB + CA;

        return ABC / 6f;
    }


    public void CalculateNormal(Triangle tri)
    {
        Vector3 Normal = Vector3.zero;

        Vector3 U = tri.b - tri.a;
        Vector3 V = tri.c - tri.a;

        Normal.x = (U.y * V.z) - (U.z * V.y);
        Normal.y = (U.z * V.x) - (U.x * V.z);
        Normal.z = (U.x * V.y) - (U.y * V.x);

        //flip it
        //tri.normal = Vector3.Normalize(Normal);
    }

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
    public void Generate(ComputeBuffer voxelBuffer, int voxelsPerAxis, float chunkSize, Vector3 worldSize, Vector3 chunkCentre, float voxelSpacing)
    {
        int kernel = DensityShader.FindKernel("NoiseDensity");

        //return value
        DensityShader.SetBuffer(0, "voxelPoints", voxelBuffer);

        //set all compute shader values
        DensityShader.SetInt("VoxelsPerAxis", voxelsPerAxis);
        DensityShader.SetVector("chunkCentre", new Vector4(chunkCentre.x, chunkCentre.y, chunkCentre.z));
        DensityShader.SetFloat("voxelSpacing", voxelSpacing);
        DensityShader.SetFloat("chunkSize", chunkSize);
        DensityShader.SetVector("worldSize", worldSize);

        //Noise Variables
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
        DensityShader.SetFloat("hurstExponent", HurstExponent);


        int MaxThreads = Mathf.CeilToInt(voxelsPerAxis / 8);

        DensityShader.Dispatch(kernel, MaxThreads, MaxThreads, MaxThreads);

    }


    void OnValidate()
    {
        settingsUpdated = true;
    }
}