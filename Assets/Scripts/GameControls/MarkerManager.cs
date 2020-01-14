using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MarkerManager : MonoBehaviour
{

    public bool PlayGame = false;
    public GameObject Collectable;
    GameObject Marker;

    [HideInInspector]
    public bool ActiveFlag = false;


    // Update is called once per frame
    void Update()
    {
        //only 
        if (PlayGame)
        {
            //spawn a flag if there isnt one already out
            if (!ActiveFlag)
            {
                //find a valid point on the mesh
                Vector3 SpawnPoint = FindPointOnMesh();

                if (SpawnPoint != Vector3.zero)
                {
                    Marker = Instantiate(Collectable, SpawnPoint + (Collectable.transform.localScale / 2.5f), Quaternion.identity);
                    ActiveFlag = true;
                }
            }
        }
        else
        {
            //clear the marker
            if (Marker != null)
            {
                Destroy(Marker);
                ActiveFlag = false;
            }

        }
    }

    //find a reasonable point on mesh to spawn an object
    Vector3 FindPointOnMesh()
    {
        //get a new location coord across x and z axis
        float visRad = GameObject.FindGameObjectWithTag("Player").GetComponent<BallControl>().visionRadius;
        Vector3 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;

        //get random direction
        Vector3 NewLocation = Random.insideUnitSphere * visRad;
        //zero out the vertical
        NewLocation = new Vector3(NewLocation.x, 0.0f, NewLocation.z);

        NewLocation += playerPos;

        //find the chunk that is in
        TerrainGeneration tG = FindObjectOfType<TerrainGeneration>();
        Vector3Int coord = Vector3Int.RoundToInt(NewLocation / tG.ChunkSize);

        //find the chunk if it already exists
        GameObject chunk = GameObject.Find($"Chunk ({coord.x}, {coord.y}, {coord.z})");

        //chunks mesh
        Mesh mesh;
        Chunk NewChunk = null;
        //if no chunk exists at that coord, create it for this purpose
        if (chunk == null)
        {
            NewChunk = tG.CreateChunkObject(coord);
            NewChunk.SetUp(false, tG.material);
            mesh = NewChunk.GetComponent<Chunk>().mesh;
            tG.UpdateTerrainGPU(NewChunk);
        }
        else
        {
            mesh = chunk.GetComponent<Chunk>().mesh;
        }

        Vector3 MarkerLocation = FindClosestVertex(mesh, NewLocation);

        //remove chunk if one was made to find mesh position
        if (chunk == null)
        {
            NewChunk.Destroy();
        }

        return MarkerLocation;
    }


    //find the closest mesh vertex to the random position generated
    Vector3 FindClosestVertex(Mesh mesh, Vector3 position)
    {
        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;

        foreach (Vector3 vertex in mesh.vertices)
        {
            Vector3 diff = position - vertex;
            float distSqr = diff.sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestVertex = vertex;
            }
        }
        return nearestVertex;
    }
}
