using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class GeneratePlaneMesh : MonoBehaviour
{

    public float size = 1;
    public int gridSize = 16;

    private MeshFilter filter;

    // Use this for initialization
    void Awake()
    {
        filter = GetComponent<MeshFilter>();
        filter.mesh = GenerateMesh();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int x = 0; x < gridSize + 1; ++x)
        {
            for (int y = 0; y < gridSize + 1; ++y)
            {
                vertices.Add(new Vector3(-size * 0.5f + size * (x / ((float)gridSize)), 0, -size * 0.5f + size * (y / ((float)gridSize))));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(x / (float)gridSize, y / (float)gridSize));
            }
        }

        var triangles = new List<int>();
        var vertCount = gridSize + 1;
        for (int i = 0; i < vertCount * vertCount - vertCount; ++i)
        {
            if ((i + 1) % vertCount == 0)
            {
                continue;
            }
            triangles.AddRange(new List<int>()
            {
                i + 1 + vertCount, i + vertCount, i,
                i, i + 1, i + vertCount + 1
            });
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);

        return mesh;
    }
}