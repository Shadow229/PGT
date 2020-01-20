using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum NoiseType
{
    Perlin,
    Simplex,
}

//[ExecuteInEditMode]
public class TerrainGeneration : MonoBehaviour
{
    [Header("GPU Utilisation")]
    [ConditionalHide(nameof(Voxelated), true, true)]
    public bool UseGPU;

    [ConditionalHide(nameof(UseGPU), true)]
    public ComputeShader MarchingCubesCompute;

    [Header("Terrain Settings")]
    public bool FixedMapSize;

    [ConditionalHide(nameof(FixedMapSize), true, true)]
    public int VerticalVisibility = 3;

    [ConditionalHide(nameof(FixedMapSize), true)]
    public Vector3Int NumOfChunks = Vector3Int.one;

    [ConditionalHide(nameof(FixedMapSize), true)]
    public bool GenerateCollisions = false;
    public int ChunkSize = 1;
    public bool showChunks = true;
    public Material material;


    [Header("Style")]
    [ConditionalHide(nameof(UseGPU), true)]
    public NoiseType noiseType = NoiseType.Perlin;
    [ConditionalHide(nameof(UseGPU),true, true)]
    public bool Voxelated;
    [ConditionalHide(nameof(Voxelated), true)]
    public int VoxelDepth = 1;

    [Space()]
    [Header("Voxel Settings")]
    public int VoxelsPerAxis = 30;
    public float ISOValue = 0;

    //public float MaxISO = 0f;
    //public float MinISO = 0f;

    [Space()]
    [Header("Noise Generation")]
    public NoiseGenerator noiseGenerator;

    [Space()]
    [Header("Terrain List")]
    //keep our chunks
    public List<Chunk> Chunks;
    //keep track of our chunks
    public Dictionary<Vector3Int, Chunk> ExistingChunks;
    //track blank chunks
    public List<Vector3> BlankChunkPositions;
    Queue<Chunk> recycleableChunks;
    //and tris
    public List<Triangle> tris;

    [HideInInspector]
    public GameObject ChunkHolder;

    //recalc flags
    bool settingsUpdated;

    //Computer Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer voxelBuffer;
    ComputeBuffer triCountBuffer;

    GameObject player;
    BallControl playerController;

    //debugging variables
    [ConditionalHide(nameof(Voxelated), true)]
    public int cubeCount;
    [ConditionalHide(nameof(Voxelated), false)]
    public int VoxelCount;
    [ConditionalHide(nameof(Voxelated), false)]
    public int NumTris;    
    public Cube cubeGizmo;



    //// Start is called before the first frame update
    void Start()
    {
        ExistingChunks = new Dictionary<Vector3Int, Chunk>();
        BlankChunkPositions = new List<Vector3>();
        tris = new List<Triangle>();
        recycleableChunks = new Queue<Chunk>();
        Chunks = new List<Chunk>();
        CreateChunkParentObject();

        //get player
        player = GameObject.FindGameObjectWithTag("Player");
        playerController = player.GetComponent<BallControl>();
    }


    // Update is called once per frame
    void Update()
    {
        if (settingsUpdated || noiseGenerator.settingsUpdated || !FixedMapSize)
        {
            //if settings have been updated 
            if (settingsUpdated || noiseGenerator.settingsUpdated)
            {
                BlankChunkPositions.Clear();
            }

            //reset the updates
            settingsUpdated = noiseGenerator.settingsUpdated = false;

            //update all chunks
            UpdateAllChunks();
        }
    }

    public void SizeChecks()
    {
        //exit early if voxels go below 1
        if (VoxelsPerAxis < 1)
        {
            VoxelsPerAxis = 1;
        }

        if (ChunkSize < 1)
        {
            ChunkSize = 1;
        }
    }

    public void UpdateAllChunks()
    {
        //saftey check on variable sizes
        SizeChecks();

        //reset helper counters
        ResetCounters();

        //create / update all chunks
        if (FixedMapSize)
        {
            CreateChunks();
        }
        else
        {
            CreateVisibleChunks();
        }


        //loop through all chunks
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

    void CreateVisibleChunks()
    {
        if (Chunks == null)
        {
            return;
        }      
        
        //position of player
        Vector3 PlayerPos = player.transform.position;

        Vector3Int ChunkOccupied = Vector3Int.RoundToInt(PlayerPos / ChunkSize);

        //get vision radius
        int PlayerVis = Mathf.CeilToInt(playerController.visionRadius);
        float sqrViewDistance = PlayerVis * PlayerVis;

        //translate to amount of chunks required to cover view radius
        int MaxChunks = Mathf.CeilToInt(PlayerVis / ChunkSize);


        //remove chunks outside of visibility
        for (int i = Chunks.Count - 1; i >= 0; i--)
        {

            Chunk chunk = Chunks[i];
            Vector3 centre = GetChunkCentre(chunk.Pos);
            Vector3 viewerOffset = PlayerPos - centre;
            Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * ChunkSize / 2;
            float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;
            Bounds bounds = new Bounds(GetChunkCentre(chunk.Pos), Vector3.one * ChunkSize);

            if (sqrDst > sqrViewDistance || !IsVisibleFrom(bounds, Camera.main) || chunk.mesh.triangles.Length == 0)
            {

                //if there's no mesh in the chunk store the position so it can be ignored next time
                if (chunk.mesh.triangles.Length == 0)
                {
                     //safety for memory getting too big - also if the count is over this there is a good chance the player has moved positions and old blanks are no longer needed
                    if (BlankChunkPositions.Count > 5000) 
                    { 
                        BlankChunkPositions.Clear(); 
                    }

                    //add to a list of blank chunks not to be created again
                    if (!BlankChunkPositions.Contains(chunk.Pos))
                    {
                        BlankChunkPositions.Add(chunk.Pos);
                    }
                }
               
                //remove it from the dictionary
                ExistingChunks.Remove(chunk.Pos);
                recycleableChunks.Enqueue(chunk);
                Chunks.RemoveAt(i);
            }
        }

        //reset number of chunks
        NumOfChunks = new Vector3Int(MaxChunks * 2 +1, VerticalVisibility * 2 +1 , MaxChunks *2 +1);

        //loop through chunks around the player and populate
        for (int x = -MaxChunks; x <= MaxChunks; x++)
        {
            for (int y = -VerticalVisibility; y <= VerticalVisibility; y++)
            {
                for (int z = -MaxChunks; z <= MaxChunks; z++)
                {
                    //coord of chunk
                    Vector3Int v3Coord = new Vector3Int(x, y, z);
                    //Move chunk coord with player chunk occupied position
                    v3Coord += ChunkOccupied;

                    //if its already there or its a known blank chunk coord - skip it
                    if (ExistingChunks.ContainsKey(v3Coord) || BlankChunkPositions.Contains(v3Coord))
                    {
                        continue;
                    }

                    //otherwise, creat it and add it to the lists
                    Vector3 centre = GetChunkCentre(v3Coord);
                    Vector3 viewerOffset = PlayerPos - centre;
                    Vector3 o = new Vector3(Mathf.Abs(viewerOffset.x), Mathf.Abs(viewerOffset.y), Mathf.Abs(viewerOffset.z)) - Vector3.one * ChunkSize / 2;
                    float sqrDst = new Vector3(Mathf.Max(o.x, 0), Mathf.Max(o.y, 0), Mathf.Max(o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance of the radius and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance)
                    {

                        Bounds bounds = new Bounds(GetChunkCentre(v3Coord), Vector3.one * ChunkSize);
                        if (IsVisibleFrom(bounds, Camera.main))
                        {
                            if (recycleableChunks.Count > 0)
                            {
                                Chunk chunk = recycleableChunks.Dequeue();

                                if(chunk)
                                {
                                    chunk.name = $"Chunk ({v3Coord.x}, {v3Coord.y}, {v3Coord.z})";
                                    chunk.Pos = v3Coord;
                                    ExistingChunks.Add(v3Coord, chunk);
                                    Chunks.Add(chunk);
                                }

                            }
                            else
                            {
                                Chunk NewChunk = CreateChunkObject(v3Coord);

                                //only generate collisions on the chunk the player is in
                                bool GenCollisions = false;
                                if (ChunkOccupied == v3Coord)
                                {
                                    GenCollisions = true;
                                }
                                NewChunk.SetUp(GenCollisions, material);

                                ExistingChunks.Add(v3Coord, NewChunk);
                                Chunks.Add(NewChunk);
                            }
                        }
                    }
                }
            }
        }
    }

    public bool IsVisibleFrom(Bounds bounds, Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
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
                        NewChunk.SetUp(GenerateCollisions, material);
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

    public Chunk CreateChunkObject(Vector3Int pos)
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
        //reset the recalc flag for that chunk
        chunk.VPA = VoxelsPerAxis;
        //physically destroy the objects
        for (int i = 0; i < chunk.Cubes.Count; i++)
        {
            chunk.Cubes[i].Destroy();
        }
        //clear down the list and dictionarys
        chunk.ExistingCubes.Clear();
        chunk.Cubes.Clear();

    }



    public void ResetCounters()
    {
        //MaxISO = 0.0f;
        //MinISO = 0.0f;
        cubeCount = 0;
        NumTris = 0;
        VoxelCount = 0;
    }


    //updating the terrain with compute shaders on the GPU
    public void UpdateTerrainGPU(Chunk chunk)
    {
        //create buffers for GPU call
        CreateBuffers();

        //int PointsPerAxis = VoxelsPerAxis + 1;
        float voxelSpacing = ChunkSize / (float) VoxelsPerAxis;
        Vector3Int coord = chunk.Pos;
        Vector3 chunkCentre = GetChunkCentre(coord);
        //get world size
        Vector3 worldSize = new Vector3(NumOfChunks.x, NumOfChunks.y, NumOfChunks.z) * ChunkSize;

        //generate the noise
        noiseGenerator.Generate(voxelBuffer, VoxelsPerAxis, ChunkSize, worldSize, chunkCentre, voxelSpacing, noiseType);
        //generate the mesh points
        MarchingCubes.UpdateTerrainGPU(MarchingCubesCompute, voxelBuffer, triangleBuffer, VoxelsPerAxis, ISOValue);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        //update the mesh with the populated triangle buffer
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        //clear the buffers immediatly
        ReleaseBuffers();

        //clear the meash ready for update
        Mesh mesh = chunk.mesh;
        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        //set verts & tris for mesh
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

        //point everything in the right direction
        mesh.RecalculateNormals();

        //rerun the setup to recalc collisions if player is in that chunk
        //position of player
        Vector3 PlayerPos = player.transform.position;
        float PlayerBase = player.GetComponent<SphereCollider>().radius;
        //chunk player is in
        Vector3Int ChunkOccupied = Vector3Int.RoundToInt((PlayerPos - new Vector3(0, PlayerBase, 0)) / ChunkSize);

        //generate all collisions on start if fixed time
        if (FixedMapSize)
        {
            chunk.SetUp(GenerateCollisions, material);
        }
        else
        {
            //only generate collisions on the chunk the player is in 
            //  (or directly above or below as this was causing mesh failures with the physics timing and player was falling through)
            bool GenCollisions = false;
            if (VectorEqu(ChunkOccupied, chunk.Pos, new Vector3(0, 1, 0)))
            {
                GenCollisions = true;
            }
            chunk.SetUp(GenCollisions, material);
        }
      

        //handle garbage collection when app stops running
        if (!Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    bool VectorEqu(Vector3 a, Vector3 b, Vector3 m)
    {
        if (a.x >= b.x - m.x && a.x <= b.x + m.x &&
            a.y >= b.y - m.y && a.y <= b.y + m.y &&
            a.z >= b.z - m.z && a.z <= b.z + m.z)
        {
            return true;
        }
        return false;
    }

    //updateing the terrain on the CPU
    public void UpdateTerrain(Chunk chunk)
    {
        //MaxISO = float.MinValue;
        //MinISO = float.MaxValue;

        //clear old data if style has been switched
        if (Voxelated)
        {
            ///clear any old mesh from non voxelated terrain
            ClearChunkMesh(chunk);
            
            ///wipe the cubes if we need to recalc the VPA
            if (chunk.VPA != VoxelsPerAxis) {TerrainCubeWipe(chunk);}
        }
        else if (chunk.Cubes.Count > 0)
        {
            //if we're no longer voxelated, remove all cubes
            TerrainCubeWipe(chunk);
        }


        float pointSpacing = ChunkSize / (float)VoxelsPerAxis;
        Vector3Int coord = chunk.Pos;
        Vector3 centre = GetChunkCentre(coord);

        tris.Clear();

        //loop through all voxel points in the chunk
        for (int x = 0; x < VoxelsPerAxis; x++)
        {
            for (int y = 0; y < VoxelsPerAxis; y++)
            {
                for (int z = 0; z < VoxelsPerAxis; z++)
                {
                    Vector3 voxCoord = new Vector3(x, y, z);

                    //back right corner of cube within chunk
                    Vector3 VoxPoint = centre + voxCoord * pointSpacing + Vector3.one * ((pointSpacing - (float)ChunkSize) / 2f);

                    if (Voxelated)
                    {
                        //determine wether to create a cube
                        UpdateCube(voxCoord, VoxPoint, pointSpacing, chunk);
                    }
                    else 
                    {                        
                        //create a terrain mesh
                        MarchingCubes.UpdateTerrain(VoxPoint);
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
        chunk.GetComponent<MeshCollider>().sharedMesh = null;
    }

    void DrawTerrainMesh(Chunk chunk)
    {
        NumTris = tris.Count;
        //apply the triangles to the mesh
        Mesh mesh = chunk.mesh;
        mesh.Clear();

        Vector3[] vertices = new Vector3[NumTris * 3];
        int[] meshTriangles = new int[NumTris * 3];

        //loop through tris and assign to mesh
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
        chunk.SetUp(GenerateCollisions, material);

    }



    void UpdateCube(Vector3 coord, Vector3 pos, float Size, Chunk ParentChunk)
    {

        //return density value from the noise generator for the current position within the chunk
        float DensityVal = noiseGenerator.GenerateDensityValue(pos);

        //store the max / min noise for debugging
        //ISOValCheck(DensityVal);

        //get the upper/lower limits for inclusion
        float upperLimit = ISOValue + (VoxelDepth * 0.5f);
        float lowerLimit = ISOValue - (VoxelDepth * 0.5f);

        bool ShouldExist = Between(DensityVal, lowerLimit, upperLimit, true);

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
                        //re init
                        ParentChunk.Cubes[i].Init(Size, coord, pos, DensityVal);
                    }
                    //exit
                    return;
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


    //create buffers to be passed between compute shader and CPU for mesh generation
    void CreateBuffers()
    {
        int PointsPerAxis = VoxelsPerAxis + 1;
        int numPoints = PointsPerAxis * PointsPerAxis * PointsPerAxis;
        int numVoxels = VoxelsPerAxis * VoxelsPerAxis * VoxelsPerAxis;
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

    //release all buffers and set values to null
    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            triangleBuffer = null;
            voxelBuffer.Release();
            voxelBuffer = null;
            triCountBuffer.Release();
            triCountBuffer = null;
        }
    }


    //helper function for between 2 float values
    public static bool Between(float num, float lower, float upper, bool inclusive = false)
    {
        return inclusive ?
            lower <= num && num <= upper :
            lower < num && num < upper;
    }

    public void UpdateCubeCount(Chunk chunk)
    {
        //update our cubecount
        cubeCount += chunk.Cubes.Count;
    }

    public void UpdatVoxelCount()
    {
        //update our cubecount
        VoxelCount = VoxelsPerAxis * VoxelsPerAxis * VoxelsPerAxis;
    }


    void OnValidate()
    {
        settingsUpdated = true;
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


    //debugging
    //public void ISOValCheck(float isoVal)
    //{
    //    if(isoVal > MaxISO) { MaxISO = isoVal; }
    //    if(isoVal < MinISO) { MinISO = isoVal; }
    //}
}