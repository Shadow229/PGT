using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerrainCube : MonoBehaviour
{
    public Vector3 Coord;
    //public float Size;
    public float NoiseVal;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;



    public Material GetDefaultMaterial()
    {    
        //assign filters and materials from a default cube
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Material mat = primitive.GetComponent<MeshRenderer>().sharedMaterial;

        DestroyImmediate(primitive);

        return mat;
    }

    public Mesh GetDefaultMeshFilter()
    {
        //assign filters and materials from a default cube
        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Mesh MF = primitive.GetComponent<MeshFilter>().sharedMesh;

        DestroyImmediate(primitive);

        return MF;
    }


    public void Init(float Size, Vector3 coord, Vector3 pos, float DensityVal)
    {
        Coord = coord;
        this.transform.position = pos;
        this.transform.localScale = Vector3.one * Size;
        NoiseVal = DensityVal;
    }

        public void SetUp(float Size, Vector3 coord, Vector3 pos, float DensityVal)
    {
        Init(Size, coord, pos, DensityVal);

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetDefaultMeshFilter();
        }

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetDefaultMaterial();
        }

        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }

    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
