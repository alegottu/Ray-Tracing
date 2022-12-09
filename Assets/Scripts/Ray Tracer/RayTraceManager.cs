using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class RayTraceManager : MonoBehaviour
{
    [SerializeField] private ComputeShader shader;
    [SerializeField] private Light dirLight;
    [SerializeField] private Transform objects;
    [SerializeField] private Transform spheres;
    [SerializeField] private Color defaultAlbedo = Color.white;
    [SerializeField] private Color defaultSpecular = Color.white;
    [SerializeField] private int reflectAmount = 2;

    private ComputeBuffer sphereBuffer;
    private ComputeBuffer meshBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer indexBuffer;
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

    private struct Mesh
    {
        public Mesh(Matrix4x4 localToWorld, Vector3 albedo, Vector3 specular, int indicesOffset, int indicesCount)
        {
            this.localToWorld = localToWorld;
            this.albedo = albedo;
            this.specular = specular;
            this.indicesOffset = indicesOffset; // where this mesh starts in the list
            this.indicesCount = indicesCount;
        }

        Matrix4x4 localToWorld;
        Vector3 albedo; 
        Vector3 specular;
        int indicesOffset;
        int indicesCount;
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

    private void GetMaterialData(Transform obj, out Vector3 albedo, out Vector3 specular)
    {
        Material material = obj.GetComponent<Renderer>().material;
        albedo = (Vector4)material.color;
        specular = material.HasColor("_SpecColor") ? (Vector4)material.GetColor("_SpecColor") : Vector3.zero;
    }

    // Gather all the sphere data from the scene
    private void GetAllSpheres()
    {
        List<Sphere> sphereList = new List<Sphere>();

        foreach (Transform child in spheres)
        {
            Vector3 albedo, specular;
            GetMaterialData(child, out albedo, out specular);
            float radius = child.GetComponent<SphereCollider>().radius;

            Sphere sphere = new Sphere(child.position, albedo, specular, radius);
            sphereList.Add(sphere);
        }

        sphereBuffer = new ComputeBuffer(sphereList.Count, 40); // 40 = 4 bytes per each float * 10 floats in a sphere
        sphereBuffer.SetData(sphereList);
    }
    
    private void GetAllMeshes()
    {
        List<Mesh> meshes = new List<Mesh>();
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();

        foreach (Transform child in objects)
        {
            UnityEngine.Mesh mesh = child.GetComponent<MeshFilter>().sharedMesh;
            int firstVertex = vertices.Count;
            vertices.AddRange(mesh.vertices);

            int firstIndex = indices.Count;
            var newIndices = mesh.GetIndices(0);
            indices.AddRange(newIndices.Select(index => index + firstVertex));
            
            Vector3 albedo, specular;
            GetMaterialData(child, out albedo, out specular);

            meshes.Add(new Mesh
            (
                child.transform.localToWorldMatrix,
                albedo,
                specular,
                firstIndex,
                newIndices.Length
            ));
        }

        meshBuffer = new ComputeBuffer(meshes.Count, 96);
        meshBuffer.SetData(meshes);
        vertexBuffer = new ComputeBuffer(vertices.Count, 12);
        vertexBuffer.SetData(vertices);
        indexBuffer = new ComputeBuffer(indices.Count, 4);
        indexBuffer.SetData(indices);
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
        shader.SetBuffer(0, "Meshes", meshBuffer);
        shader.SetBuffer(0, "Vertices", vertexBuffer);
        shader.SetBuffer(0, "Indices", indexBuffer);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        skybox = Resources.Load("Textures/skybox") as Texture2D;

        NewTexture();
        GetAllSpheres();
        GetAllMeshes();
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

    private void OnDestroy()
    {
        target.Release();
        sphereBuffer.Dispose();
        meshBuffer.Dispose();
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }
}
