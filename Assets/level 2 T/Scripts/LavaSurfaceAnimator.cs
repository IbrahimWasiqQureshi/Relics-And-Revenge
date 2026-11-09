using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class LavaSurfaceAnimator : MonoBehaviour
{
    [Header("Surface Shape")]
    [Min(2)] public int resolution = 80;
    [Min(1f)] public float size = 50f;
    [Min(0f)] public float waveHeight = 0.32f;
    [Min(0.01f)] public float waveScale = 0.22f;
    [Min(0f)] public float waveSpeed = 0.65f;

    [Header("Visual Flow")]
    public Material lavaMaterial;
    public Vector2 textureFlow = new Vector2(0.03f, 0.015f);

    private Mesh mesh;
    private MeshCollider surfaceCollider;
    private Vector3[] vertices;
    private Vector2[] uvs;
    private float colliderTimer;

    private void Awake()
    {
        BuildMesh();

        if (lavaMaterial != null)
        {
            GetComponent<MeshRenderer>().material = lavaMaterial;
        }
    }

    private void BuildMesh()
    {
        int vertexCount = (resolution + 1) * (resolution + 1);
        vertices = new Vector3[vertexCount];
        uvs = new Vector2[vertexCount];
        int[] triangles = new int[resolution * resolution * 6];

        float halfSize = size * 0.5f;
        for (int z = 0, i = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++, i++)
            {
                float u = x / (float)resolution;
                float v = z / (float)resolution;
                vertices[i] = new Vector3(Mathf.Lerp(-halfSize, halfSize, u), 0f, Mathf.Lerp(-halfSize, halfSize, v));
                uvs[i] = new Vector2(u * 5f, v * 5f);
            }
        }

        for (int z = 0, t = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++, t += 6)
            {
                int a = z * (resolution + 1) + x;
                int b = a + resolution + 1;
                triangles[t] = a;
                triangles[t + 1] = b;
                triangles[t + 2] = a + 1;
                triangles[t + 3] = a + 1;
                triangles[t + 4] = b;
                triangles[t + 5] = b + 1;
            }
        }

        mesh = new Mesh { name = "Animated Lava Surface" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        surfaceCollider = GetComponent<MeshCollider>();
        surfaceCollider.sharedMesh = mesh;
    }

    private void Update()
    {
        float time = Time.time * waveSpeed;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            float broadWave = Mathf.Sin((vertex.x * waveScale) + time) * 0.55f;
            float crossWave = Mathf.Cos((vertex.z * waveScale * 1.35f) - (time * 1.25f)) * 0.3f;
            float detail = Mathf.PerlinNoise(vertex.x * 0.12f + time * 0.18f, vertex.z * 0.12f - time * 0.13f) - 0.5f;
            vertex.y = (broadWave + crossWave + detail) * waveHeight;
            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        colliderTimer += Time.deltaTime;
        if (colliderTimer > 0.1f)
        {
            surfaceCollider.sharedMesh = null;
            surfaceCollider.sharedMesh = mesh;
            colliderTimer = 0f;
        }

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material material = renderer.material;
        Vector2 offset = Time.time * textureFlow;
        if (material.HasProperty("_BaseMap"))
            material.SetTextureOffset("_BaseMap", offset);
        if (material.HasProperty("_MainTex"))
            material.SetTextureOffset("_MainTex", offset);
    }
}