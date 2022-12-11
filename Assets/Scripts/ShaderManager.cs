using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Camera))]
public class ShaderManager : MonoBehaviour
{
    [SerializeField] protected ComputeShader shader;
    [SerializeField] protected Light dirLight;
    [SerializeField] protected Transform objects;
    [SerializeField] protected Transform spheres;
    [SerializeField] protected Color defaultAlbedo = Color.white;
    [SerializeField] protected Color defaultSpecular = Color.white;
    [SerializeField] protected Color defaultShadow = Color.black;
    [SerializeField] protected int reflectAmount = 2;

    protected ComputeBuffer sphereBuffer;
    protected ComputeBuffer meshBuffer;
    protected ComputeBuffer vertexBuffer;
    protected ComputeBuffer indexBuffer;
    protected RenderTexture target = null;
    protected Camera cam;

    protected struct Sphere
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

    protected struct Mesh
    {
        public Mesh(Matrix4x4 localToWorld, Vector3 position, Vector3 albedo, Vector3 specular, float radius, int indicesOffset, int indicesCount)
        {
            this.localToWorld = localToWorld;
            this.position = position;
            this.albedo = albedo;
            this.specular = specular;
            this.radius = radius;
            this.indicesOffset = indicesOffset; // where this mesh starts in the list
            this.indicesCount = indicesCount;
        }

        Matrix4x4 localToWorld;
        Vector3 position;
        Vector3 albedo; 
        Vector3 specular;
        float radius;
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
            SphereCollider collider = child.GetComponent<SphereCollider>();
            Vector3 position = child.position + collider.center;
            float radius = collider.bounds.extents.magnitude;

            meshes.Add(new Mesh
            (
                child.transform.localToWorldMatrix,
                position,
                albedo,
                specular,
                radius,
                firstIndex,
                newIndices.Length
            ));
        }

        meshBuffer = new ComputeBuffer(meshes.Count, 112);
        meshBuffer.SetData(meshes);
        vertexBuffer = new ComputeBuffer(vertices.Count, 12);
        vertexBuffer.SetData(vertices);
        indexBuffer = new ComputeBuffer(indices.Count, 4);
        indexBuffer.SetData(indices);
    }

    private void SetStaticShaderParameters()
    {
        shader.SetMatrix("InverseProjection", cam.projectionMatrix.inverse);
        Vector3 light = dirLight.transform.forward;
        shader.SetVector("DirectionalLight", new Vector4(light.x, light.y, light.z, dirLight.intensity));
        shader.SetVector("DefaultAlbedo", defaultAlbedo);
        shader.SetVector("DefaultSpecular", defaultSpecular);
        shader.SetVector("DefaultShadow", defaultShadow);
        shader.SetInt("ReflectAmount", reflectAmount);
        shader.SetBuffer(0, "Spheres", sphereBuffer);
        shader.SetBuffer(0, "Meshes", meshBuffer);
        shader.SetBuffer(0, "Vertices", vertexBuffer);
        shader.SetBuffer(0, "Indices", indexBuffer);
    }

    protected void ShaderSetup()
    {
        cam = GetComponent<Camera>();

        NewTexture();
        GetAllSpheres();
        GetAllMeshes();
        SetStaticShaderParameters();
    }

    private void SetDynamicShaderParameters()
    {
        shader.SetMatrix("CameraToWorld", cam.cameraToWorldMatrix);
    }

    protected void Render()
    {
        SetDynamicShaderParameters();

        shader.SetTexture(0, "Result", target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 22);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 22);
        shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
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
