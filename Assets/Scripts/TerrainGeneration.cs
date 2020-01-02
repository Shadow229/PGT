using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

//[ExecuteInEditMode]
public class TerrainGeneration : MonoBehaviour
{
    [Header("GPU Utilisation")]
    public bool UseGPU;

    [ConditionalHide(nameof(UseGPU), true)]
    public ComputeShader MarchingCubesCompute;

    [Header("Terrain Settings")]
    public bool FixedMapSize;

    [ConditionalHide(nameof(FixedMapSize), true)]
    public Vector3Int NumOfChunks = Vector3Int.one;
    public int ChunkSize = 1;
    public bool showChunks = true;
    //public Vector3 PlayerPos = Vector3.zero;
    //public float VisibilityRadius = 5f;

    [Header("Style")]
    public bool Voxelated;

    [Space()]
    [Header("Voxel Settings")]
    public int VoxelsPerAxis = 30;
    public float ISOValue = 0;
    public float MaxISO = 0f;
    public float MinISO = 0f;

    [Space()]
    [Header("Noise Generation")]
    public NoiseGenerator noiseGenerator;

    [Space()]
    [Header("Terrain List")]
    //keep our chunks
    public List<Chunk> Chunks;
    //keep our cubes

    //keep track of our chunks
    public Dictionary<Vector3Int, Chunk> ExistingChunks;

    [HideInInspector]
    public GameObject ChunkHolder;

    //recalc flags
    bool settingsUpdated;
    int VPACalc = 0;

    public int NumTris = 0;

    public List<Triangle> tris;

    //Computer Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer voxelBuffer;
    ComputeBuffer triCountBuffer;

    //debugging variables

    public int cubeCount;
    public Cube cubeGizmo;



    //// Start is called before the first frame update
    void Start()
    {
        ExistingChunks = new Dictionary<Vector3Int, Chunk>();
        tris = new List<Triangle>();
        CreateChunkParentObject();
        VPACalc = VoxelsPerAxis;
    }


    // Update is called once per frame
    void Update()
    {
        if (settingsUpdated || noiseGenerator.settingsUpdated)
        {
            settingsUpdated = noiseGenerator.settingsUpdated = false;

            UpdateAllChunks();
        }
    }

    public void UpdateAllChunks()
    {
        cubeCount = 0;

        CreateChunks();

        List<Chunk> Chunks = (this.Chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>()) : this.Chunks;
        foreach (var chunk in Chunks)
        {
            if (UseGPU)
            {
                //compute shader terrain update
                UpdateTerrainGPU(chunk);
            }
            else
            {
                //CPU terrain update
                UpdateTerrain(chunk);
            }

            UpdateCubeCount(chunk);
        }
    }


    public void UpdateCubeCount(Chunk chunk)
    {
        //update our cubecount
        cubeCount += chunk.Cubes.Count;
    }

    void CreateChunkParentObject()
    {
        // Create/find mesh holder object for organizing chunks under in the hierarchy
        if (ChunkHolder == null)
        {
            if (GameObject.Find("ChunkHolder"))
            {
                ChunkHolder = GameObject.Find("ChunkHolder");
            }
            else
            {
                ChunkHolder = new GameObject("ChunkHolder");
            }
        }
    }


    //updates/destroys and creates new chunks if required
    void CreateChunks()
    {
        Chunks = new List<Chunk>();
        //get all of our chunks
        List<Chunk> oldChunks = new List<Chunk>(FindObjectsOfType<Chunk>());


        for (int x = 0; x < NumOfChunks.x; x++)
        {
            for (int y = 0; y < NumOfChunks.y; y++)
            {
                for (int z = 0; z < NumOfChunks.z; z++)
                {
                    Vector3Int v3Coord = new Vector3Int(x, y, z);
                    bool ChunkExists = false;

                    //check if our terrain already exists at the v3coord
                    for (int i = 0; i < oldChunks.Count; i++)
                    {
                        if (oldChunks[i].Pos == v3Coord)
                        {
                            ChunkExists = true;
                            //store that chunk spot into the list
                            Chunks.Add(oldChunks[i]);
                            //remove it from the found list
                            oldChunks.RemoveAt(i);
                        }
                    }

                    if (!ChunkExists)
                    {
                        Chunk NewChunk = CreateChunkObject(v3Coord);
                        NewChunk.SetUp();
                        Chunks.Add(NewChunk);
                    }
                }
            }
        }

        // Delete any remaining old terrain
        for (int i = 0; i < oldChunks.Count; i++)
        {
            //remove it from the dictionary
            ExistingChunks.Remove(oldChunks[i].Pos);
            //clear the cubes logs
            oldChunks[i].ExistingCubes.Clear();
            //destroy the chunk
            oldChunks[i].Destroy();
        }



    }

    Chunk CreateChunkObject(Vector3Int pos)
    {
        GameObject chunk = new GameObject($"Chunk ({pos.x}, {pos.y}, {pos.z})");
        chunk.transform.parent = ChunkHolder.transform;
        Chunk newChunk = chunk.AddComponent<Chunk>();
        newChunk.Pos = pos;
        return newChunk;
    }



    Vector3 GetChunkCentre(Vector3Int coord)
    {
        return new Vector3(coord.x, coord.y, coord.z) * ChunkSize;
    }

    public void TerrainCubeWipe(Chunk chunk)
    {
        //reset the recalc flag
        VPACalc = VoxelsPerAxis;
        //physically destroy the objects
        for (int i = 0; i < chunk.Cubes.Count; i++)
        {
            chunk.Cubes[i].Destroy();
        }
        //clear down the list and dictionarys
        chunk.ExistingCubes.Clear();
        chunk.Cubes.Clear();

    }

    public void ISOValCheck(float isoVal)
    {
        if(isoVal > MaxISO) { MaxISO = isoVal; }
        if(isoVal < MinISO) { MinISO = isoVal; }
    }


    //updating the terrain with compute shaders on the GPU
    public void UpdateTerrainGPU(Chunk chunk)
    {
        //create buffers for GPU call
        CreateBuffers();

        float voxelSpacing = ChunkSize / (VoxelsPerAxis - 1);
        Vector3Int coord = chunk.Pos;
        Vector3 chunkCentre = GetChunkCentre(coord);
        //get world size
        Vector3 worldSize = new Vector3(NumOfChunks.x, NumOfChunks.y, NumOfChunks.z) * ChunkSize;

        //generate the noise
        noiseGenerator.Generate(voxelBuffer, VoxelsPerAxis, ChunkSize, worldSize, chunkCentre, voxelSpacing);
        //generate the mesh points
       // MarchingCubes.UpdateTerrainGPU(MarchingCubesCompute, voxelBuffer, triangleBuffer, VoxelsPerAxis, ISOValue);

        int kernel = MarchingCubesCompute.FindKernel("CubeMarch");
        int MaxThreads = Mathf.CeilToInt(VoxelsPerAxis / 8);

        triangleBuffer.SetCounterValue(0);
        MarchingCubesCompute.SetBuffer(kernel, "voxelPoints", voxelBuffer);
        MarchingCubesCompute.SetBuffer(kernel, "triangles", triangleBuffer);
        MarchingCubesCompute.SetInt("voxelsPerAxis", VoxelsPerAxis);
        MarchingCubesCompute.SetFloat("ISOValue", ISOValue);

        MarchingCubesCompute.Dispatch(kernel, MaxThreads, MaxThreads, MaxThreads);


        //update the mesh with the populated triangle buffer
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }


    //updateing the terrain on the CPU
    public void UpdateTerrain(Chunk chunk)
    {
        float fCS = ChunkSize * 1.0f;
        float fNV = VoxelsPerAxis * 1.0f;

        MaxISO = float.MinValue;
        MinISO = float.MaxValue;

        //clear old data if style has been switched
        if (Voxelated)
        {
            ///clear any old mesh from non voxelated terrain
            ClearChunkMesh(chunk);
            ///wipe the cubes if we need to recalc the VPA
            if (VPACalc != VoxelsPerAxis) {TerrainCubeWipe(chunk);}
        }
        else if (chunk.Cubes.Count > 0)
        {
            TerrainCubeWipe(chunk);
        }

        float pointSpacing = fCS / fNV;

        Vector3Int coord = chunk.Pos;
        Vector3 centre = GetChunkCentre(coord);

        tris.Clear();

        //loop through all desired voxel points in the chunk
        for (int x = 0; x < VoxelsPerAxis; x++)
        {
            for (int y = 0; y < VoxelsPerAxis; y++)
            {
                for (int z = 0; z < VoxelsPerAxis; z++)
                {
                    Vector3 voxCoord = new Vector3(x, y, z);

                    Vector3 VoxPoint = centre + voxCoord * pointSpacing + Vector3.one * ((pointSpacing - (float)ChunkSize) / 2f);

                    if (Voxelated)
                    {
                        UpdateCubes(voxCoord, VoxPoint, pointSpacing, chunk);
                    }
                    else //create terrain info
                    {                        
                        //create a terrain mesh
                        MarchingCubes.UpdateTerrain(chunk, VoxPoint);
                    }
                }
            }
        }
        //draw the mesh once we've calculated all the tris
        if (!Voxelated) DrawTerrainMesh(chunk);
    }


    void ClearChunkMesh(Chunk chunk)
    {
        chunk.mesh.Clear();
    }

    void DrawTerrainMesh(Chunk chunk)
    {
        NumTris = tris.Count;
        //apply the triangles to the mesh
        Mesh mesh = chunk.mesh;
        mesh.Clear();

        Vector3[] vertices = new Vector3[NumTris * 3];
        Vector3[] normals = new Vector3[NumTris];
        int[] meshTriangles = new int[NumTris * 3];


        for (int i = 0; i < NumTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;


        noiseGenerator.ReverseNormals(mesh);
        mesh.RecalculateNormals();
        //rerun the setup to recalc collisions
        chunk.SetUp();

    }



    void UpdateCubes(Vector3 coord, Vector3 pos, float Size, Chunk ParentChunk)
    {

        //return density value from the noise generator for the current position within the chunk
        int DensityVal = Mathf.FloorToInt(noiseGenerator.GenerateDensityValue(pos));

        //store the max / min noise for debugging
        ISOValCheck(DensityVal);

        bool ShouldExist = DensityVal == ISOValue ? true : false;

        //check if our cube already exists at that position
        if (ParentChunk.ExistingCubes.ContainsKey(coord))
        {
            //loop though and find it
            for (int i = 0; i < ParentChunk.Cubes.Count; i++)
            {
                if (ParentChunk.Cubes[i].Coord == coord)
                {
                    //check if its inside the isosuface val
                    if (!ShouldExist)
                    {
                        //remove it from the dictionary
                        ParentChunk.ExistingCubes.Remove(coord);
                        //destroy it
                        ParentChunk.Cubes[i].Destroy();
                        //remote it from the list
                        ParentChunk.Cubes.RemoveAt(i);
                    }
                    else
                    {
                        ParentChunk.Cubes[i].Init(Size, coord, pos, DensityVal);
                    }
                }
            }
        }
        else if (ShouldExist)
        {
            TerrainCube NewCube = CreateCubeObject(coord, pos, Size, DensityVal, ParentChunk);
            //add it to the list
            ParentChunk.Cubes.Add(NewCube);
            //record it in the dictionary
            ParentChunk.ExistingCubes.Add(coord, NewCube);
        }
    }


    TerrainCube CreateCubeObject(Vector3 coord, Vector3 pos, float Size, float Density, Chunk ParentChunk)
    {
        GameObject cube = new GameObject($"cube ({coord.x}, {coord.y}, {coord.z})");
        cube.AddComponent<TerrainCube>();
        cube.transform.parent = ParentChunk.transform;
        TerrainCube TC = cube.GetComponent<TerrainCube>();
        TC.SetUp(Size, coord, pos, Density);
        return TC;
    }



    void OnValidate()
    {
        settingsUpdated = true;
    }



    void CreateBuffers()
    {
        int numPoints = VoxelsPerAxis * VoxelsPerAxis * VoxelsPerAxis;
        int numVoxelsPerAxis = VoxelsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || (voxelBuffer == null || numPoints != voxelBuffer.count))
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            voxelBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        }
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            voxelBuffer.Release();
            triCountBuffer.Release();
        }
    }


    void OnDrawGizmos()
    {
        if (showChunks)
        {
            Gizmos.color = Color.white;

            List<Chunk> Chunks = (this.Chunks == null) ? new List<Chunk>(FindObjectsOfType<Chunk>()) : this.Chunks;

            foreach (var chunk in Chunks)
            {
                if (chunk)
                {
                    Gizmos.DrawWireCube(GetChunkCentre(chunk.Pos), Vector3.one * ChunkSize);
                }
            }
        }


        //for (int i = 0; i < 8; i++)
        //{
        //    Gizmos.color = Color.Lerp(Color.white, Color.black, i / 7f);

        //    Vector3 pos = new Vector3(cubeGizmo.CornerPos[i].x, cubeGizmo.CornerPos[i].y, cubeGizmo.CornerPos[i].z);

        //    Gizmos.DrawSphere(pos, 0.1f);
        //}
    }
}
