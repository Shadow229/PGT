using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    public Dictionary<Vector3, TerrainCube> ExistingCubes;
    public List<TerrainCube> Cubes;

    [HideInInspector]
    public Mesh mesh;
    public Vector3Int Pos;


    public Material GetDefaultMaterial()
    {
        //assign filters and materials from a default cube
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Material mat = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        DestroyImmediate(primitive);

        return mat;
    }



    public void SetUp()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        ExistingCubes = new Dictionary<Vector3, TerrainCube>();
        Cubes = new List<TerrainCube>();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetDefaultMaterial();
        }

        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
        }


        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh = mesh;
        }

        meshCollider.sharedMesh = mesh;
        // force update
        meshCollider.enabled = false;
        meshCollider.enabled = true;


      


    }

    public void Destroy()
    {
        Destroy(this.gameObject);
    }

}
