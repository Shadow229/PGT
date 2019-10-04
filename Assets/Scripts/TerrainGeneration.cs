using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class TerrainGeneration : MonoBehaviour
{
    public Vector3Int TerrainSize = Vector3Int.one;
    [Range(-100, 100)]
    public int ISOValue = 0;
    public float MaxISO = 0f;
    public float MinISO = 0f;
    public float ISOVal = 0f;

    //keep our cubes
    public List<TerrainCube> TerrainCubes;
    //keep track of our cubes
    public Dictionary<Vector3Int, TerrainCube> ExistingCubes;

    public NoiseGenerator noiseGenerator;

    [HideInInspector]
    public GameObject TerrainHolder;

    //// Start is called before the first frame update
    void Start()
    {
        ExistingCubes = new Dictionary<Vector3Int, TerrainCube>();
        CreateTerrainParentObject();
    }


    // Update is called once per frame
    void Update()
    {
        UpdateTerrain();
    }

    public void UpdateTerrain()
    {
        MaxISO = float.MinValue;
        MinISO = float.MaxValue;
    TerrainCubes = new List<TerrainCube>();
        //get all of our cubes
        List<TerrainCube> oldTerrain = new List<TerrainCube>(FindObjectsOfType<TerrainCube>());

        for (int x = 0; x < TerrainSize.x; x++)
        {
            for (int y = 0; y < TerrainSize.y; y++)
            {
                for (int z = 0; z < TerrainSize.z; z++)
                {
                    Vector3Int v3Coord = new Vector3Int(x, y, z);
                    bool TerrainExists = false;

                    //  float noiseVal = noiseGenerator.Perlin3D((x * 0.1f), (y * 0.1f), (z * 0.1f)) * 100;
                    float noiseVal = noiseGenerator.GenerateDensityValue(new Vector3Int(x, y, z));

                    //debug purposes
                    if (noiseVal < MinISO) MinISO = noiseVal;
                    if (noiseVal > MaxISO) MaxISO = noiseVal;
                    ISOVal = ISOValue * 0.1f;

                    bool ShouldExist = noiseVal > ISOVal ? true : false;

                    //check if our terrain already exists at the v3coord
                    for (int i = 0; i < oldTerrain.Count; i++)
                    {
                        if (oldTerrain[i].Coord == v3Coord)
                        {
                            if (ShouldExist)
                            {
                                //store that terrain spot into the list
                                TerrainCubes.Add(oldTerrain[i]);
                                oldTerrain[i].NoiseVal = noiseVal;
                            }
                            else
                            {
                                //remove it from the dictionary
                                ExistingCubes.Remove(v3Coord);
                                //Destroy it
                                oldTerrain[i].Destroy();
                            }

                            TerrainExists = true;
                            //remove it from the found list
                            oldTerrain.RemoveAt(i);

                        }
                    }

                    if (!TerrainExists && ShouldExist)
                    {
                        TerrainCube NewTerrain = CreateTerrain(v3Coord);
                        NewTerrain.SetUp();
                        NewTerrain.NoiseVal = noiseVal;
                        ExistingCubes.Add(v3Coord, NewTerrain);
                    }

                }

            }

        }

        // Delete any remaining old terrain
        for (int i = 0; i < oldTerrain.Count; i++)
        {                                
            //remove it from the dictionary
            ExistingCubes.Remove(oldTerrain[i].Coord);
            oldTerrain[i].Destroy();
        }
    }




    TerrainCube CreateTerrain(Vector3Int coord)
    {
        GameObject Cube = new GameObject($"Cube ({coord.x}, {coord.y}, {coord.z})");
        Cube.transform.position = new Vector3(coord.x, coord.y, coord.z);
        TerrainCube newTerrain = Cube.AddComponent<TerrainCube>();
        newTerrain.transform.parent = TerrainHolder.transform;
        newTerrain.Coord = coord;
        return newTerrain;
    }




    void CreateTerrainParentObject()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (TerrainHolder == null)
        {
            if (GameObject.Find("TerrainHolder"))
            {
                TerrainHolder = GameObject.Find("TerrainHolder");
            }
            else
            {
                TerrainHolder = new GameObject("TerrainHolder");
            }
        }
    }

}
