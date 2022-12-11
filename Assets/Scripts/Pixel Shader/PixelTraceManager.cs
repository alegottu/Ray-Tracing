using UnityEngine;

public class PixelTraceManager : ShaderManager
{
    [SerializeField] private int pixelSize = 1;

    private void Awake()
    {
        ShaderSetup();
        shader.SetInt("PixelSize", pixelSize);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Render();
        Graphics.Blit(target, dest);
    }
}