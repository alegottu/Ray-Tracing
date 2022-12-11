using UnityEngine;

public class PixelTraceManager : ShaderManager
{
    [SerializeField] private int targetWidth = 1;

    private void Awake()
    {
        ShaderSetup();

        //float aspectRatio = Screen.width / Screen.height;
        // Screen.SetResolution(targetWidth, Mathf.CeilToInt(targetWidth / aspectRatio), FullScreenMode.FullScreenWindow);
        int pixelSize = Mathf.CeilToInt(Screen.width / targetWidth);
        shader.SetInt("PixelSize", pixelSize);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Render();
        Graphics.Blit(target, dest);
    }
}