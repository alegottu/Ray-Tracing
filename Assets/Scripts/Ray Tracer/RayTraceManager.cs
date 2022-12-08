using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class RayTraceManager : MonoBehaviour
{
    [SerializeField] private ComputeShader shader;
    [SerializeField] private Light dirLight;
    [SerializeField] private Transform spheres;
    [SerializeField] private Color defaultAlbedo = Color.white;
    [SerializeField] private Color defaultSpecular = Color.white;
    [SerializeField] private int reflectAmount = 2;

    private ComputeBuffer sphereBuffer;
    private RenderTexture target = null;
    private Camera cam;
    private Texture2D skybox;
    private Material AAMaterial;
    private uint currentSample = 0;

    private struct Sphere
    {
        public Sphere(Vector3 position, Vector3 albedo, Vector3 specular, float radius)
        {
            this.position = position;
            this.albedo = albedo;
            this.specular = specular;
            this.radius = radius;
        }

        Vector3 position;
        Vector3 albedo;
        Vector3 specular;
        float radius;
    }

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

    // Gather all the sphere data from the scene
    private void GetAllSpheres()
    {
        List<Sphere> sphereList = new List<Sphere>();

        foreach (Transform child in spheres)
        {
            Material material = child.GetComponent<Renderer>().material;
            Vector3 albedo = (Vector4)material.color;
            Vector3 specular = material.HasColor("_SpecColor") ? (Vector4)material.GetColor("_SpecColor") : Vector3.zero;
            float radius = child.GetComponent<SphereCollider>().radius;

            Sphere sphere = new Sphere(child.position, albedo, specular, radius);
            sphereList.Add(sphere);
        }

        sphereBuffer = new ComputeBuffer(sphereList.Count, 40); // 40 = 4 bytes per each float * 10 floats in a sphere
        sphereBuffer.SetData(sphereList);
    }

    private void SetStaticShaderParameters()
    {
        shader.SetTexture(0, "Skybox", skybox);
        Vector3 light = dirLight.transform.forward;
        shader.SetVector("DirectionalLight", new Vector4(light.x, light.y, light.z, dirLight.intensity));
        shader.SetVector("DefaultAlbedo", defaultAlbedo);
        shader.SetVector("DefaultSpecular", defaultSpecular);
        shader.SetInt("ReflectAmount", reflectAmount);
        shader.SetBuffer(0, "Spheres", sphereBuffer);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        skybox = Resources.Load("Textures/skybox") as Texture2D;

        NewTexture();
        GetAllSpheres();
        SetStaticShaderParameters();

        AAMaterial = new Material(Shader.Find("Hidden/AntiAlias"));
    }

    private void SetDynamicShaderParameters()
    {
        shader.SetMatrix("CameraToWorld", cam.cameraToWorldMatrix);
        shader.SetMatrix("InverseProjection", cam.projectionMatrix.inverse);
        shader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        SetDynamicShaderParameters();

        shader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

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
