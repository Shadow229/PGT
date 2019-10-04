using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{


    public int Seed = 0;
    // Noise settings
    public float OffsetX, OffsetY, OffsetZ;
    public int Octaves;

    public float NoiseScale = 2.5f;
    public float NoiseWeight = 2f;

    //public float FloorOffset;
    public float WeightMultiplier;
    //public bool CloseEdges;
    public float HardFloor;
    public float HardFloorWeight;

    [Range(0f, 1f)]
    public float HurstExponent = 0.5f;

    [Range(1.9f, 2.1f)]
    public float Lacunarity = 2f;

    public float Persistence = 1f;
    


    public float GenerateDensityValue(Vector3Int pos)
    {
        var Rng = new System.Random(Seed);

        Vector3 SeededPos = new Vector3(pos.x * (float)Rng.NextDouble(), pos.y * (float)Rng.NextDouble(), pos.z * (float)Rng.NextDouble());

        Vector3 Offset = new Vector3(OffsetX, OffsetY, OffsetZ);
        Vector3 v = (Vector3)SeededPos + Offset;

        float c = FBM(v);

        return c;
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

        float fv = noise * NoiseWeight;

        if (vec.y < HardFloor)
        {
            fv += (HardFloorWeight / 100);
        }

        return fv;
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

}