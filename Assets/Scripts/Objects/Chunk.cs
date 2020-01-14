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

    [HideInInspector]
    public int VPA = 0;


    public Material GetDefaultMaterial()
    {
        //assign filters and materials from a default cube
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Material mat = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        DestroyImmediate(primitive);

        return mat;
    }


    //set up a chunk with the relevent mesh components
    public void SetUp(bool Collisions, Material material = null)
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
        }

        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            meshFilter.sharedMesh = mesh;
        }

        if (Collisions)
        {
            if (meshCollider.sharedMesh == null)
            {
                meshCollider.sharedMesh = mesh;
            }
        }
        else
        {
            meshCollider.sharedMesh = null;
        }
         

        // force update
        meshCollider.enabled = false;
        meshCollider.enabled = true;

        if (material == null)
        {
            meshRenderer.material = GetDefaultMaterial();
        }
        else
        {
            meshRenderer.material = material;
        }
    }

    public void Destroy()
    {
        DestroyImmediate(this.gameObject);
    }

}
