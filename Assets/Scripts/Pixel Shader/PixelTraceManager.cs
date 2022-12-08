using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelTraceManager : MonoBehaviour
{
    [SerializeField] private ComputeShader shader;
    [SerializeField] private int pixelSize = 1;

    private RenderTexture target = null;
    private Camera cam;
    private Texture2D skybox;

    private void NewTexture()
    {
        target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        target.enableRandomWrite = true;
        target.Create();
    }

    private void InitTexture()
    {
        if (target == null)
        {
            NewTexture();
        }
        else if (target.width != Screen.width || target.height != Screen.height)
        {
            target.Release();
            NewTexture();
        }
    }

    private void SetStaticShaderParameters()
    {
        shader.SetTexture(0, "Skybox", skybox);
        shader.SetInt("PixelSize", pixelSize);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        skybox = Resources.Load("Textures/skybox") as Texture2D;

        NewTexture();
        SetStaticShaderParameters();
    }

    private void SetDynamicShaderParameters()
    {
        shader.SetMatrix("CameraToWorld", cam.cameraToWorldMatrix);
        shader.SetMatrix("InverseProjection", cam.projectionMatrix.inverse);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        SetDynamicShaderParameters();

        shader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(target, dest);
    }
}
