using UnityEngine;

public class RayTraceManager : ShaderManager
{
    private Texture2D skybox;
    private Material AAMaterial;
    private uint currentSample = 0;

    private void Awake()
    {
        skybox = Resources.Load("Textures/skybox") as Texture2D;
        ShaderSetup();
        shader.SetTexture(0, "Skybox", skybox);
        AAMaterial = new Material(Shader.Find("Hidden/AntiAlias"));
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Render();
        shader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));

        AAMaterial.SetFloat("Sample", currentSample);
        Graphics.Blit(target, dest, AAMaterial);
        currentSample++;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }
}
