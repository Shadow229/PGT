using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class TextureGenerator : MonoBehaviour
{
    [Header("Fog")]
    public bool ShowFog = false;
    [Range(0, 1)]
    public float fogDstMultiplier = 1;

    [Space()]
    [Header("Texturing")]
    public bool TextureMesh = false;
    public Material mat;
    public Gradient gradient;
    public Vector2 TextureStretch;

    //private variables
    BallControl playerController;
    Camera cam;
    Texture2D texture;
    const int textureResolution = 50;
    Light directionalLight;
    Light playerLight;

    private void Start()
    {
        //get the light components to save searching for them at runtime
        directionalLight = GameObject.Find("Directional Light").GetComponent<Light>();
        playerLight = GameObject.Find("HighLightSpot").GetComponent<Light>();
    }

    void Init()
    {
        if (texture == null || texture.width != textureResolution)
        {
            texture = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false);
        }
    }

    void Update()
    {
        TerrainGeneration m = FindObjectOfType<TerrainGeneration>();
        float boundsY = m.ChunkSize * m.NumOfChunks.y;
        boundsY += boundsY * 0.2f; //20% buffer to ensure texture doesnt wrap

        //update textures
        if (TextureMesh)
        {
            Init();
            UpdateTexture();

            //set texture
            mat.SetFloat("boundsY", boundsY);
            mat.SetTexture("ramp", texture);
            Vector4 shaderParams = new Vector4(TextureStretch.x, TextureStretch.y, 0.0f, 0.0f);
            mat.SetVector("params", shaderParams);
        }
        else
        {
            //new blank texture
            Texture2D tex = new Texture2D(textureResolution, 1, TextureFormat.RGBA32, false); ;

            mat.SetFloat("boundsY", boundsY);
            mat.SetTexture("ramp", tex);
            mat.SetVector("params", Vector3.zero);
        }

        //update fog
        if (ShowFog)
        {
            if (playerController == null)
            {
                playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<BallControl>();
            }
            if (cam == null)
            {
                cam = FindObjectOfType<Camera>();
            }

            //enable and set the fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = cam.backgroundColor;
            RenderSettings.fogEndDistance = playerController.visionRadius * fogDstMultiplier * 4;

            //turn up the directional light
            directionalLight.intensity = 2.3f;
            playerLight.intensity = 23.0f;
        }
        else
        {
            //disable the fog
            RenderSettings.fog = false;
            //turn down the directional light
            directionalLight.intensity = 1.0f;
            playerLight.intensity = 3.0f;
        }


    }

    void UpdateTexture()
    {
        if (gradient != null)
        {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++)
            {
                Color gradientCol = gradient.Evaluate(i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels(colours);
            texture.Apply();
        }
    }
 
}
