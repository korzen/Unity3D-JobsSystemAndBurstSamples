
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel;

/// <summary>
/// An optimized ocean simulation using double buffering to overlap vertex modification with normals calculations
/// </summary>
public unsafe class OceanDoubleBuffered : MonoBehaviour
{
    const int READ = 0;
    const int WRITE = 1;

    [Range(0.0f, 10f)]
    [SerializeField]
    protected float m_PerlinStrength = 0.25f;


    [Range(0.0f, 10f)]
    [SerializeField]
    protected float m_RippleStrength = 0.25f;

    [Range(0.0f, 2f)]
    [SerializeField]
    protected float m_TimeMult = 1f;

    [Range(0.0f, 10f)]
    [SerializeField]
    protected float m_Scale = 1f;

    
    NativeArray<Vector3>[] m_Vertices;
    NativeArray<Vector3> m_Normals;
    NativeArray<Vector3> m_TriNormals;

    NativeArray<int> m_VerticesToTrianglesMapping;
    NativeArray<int> m_VerticesToTrianglesMappingCount;
    NativeArray<int> m_Indices;

    Vector3[] m_ModifiedVertices;
    Vector3[] m_ModifiedNormals;

    JobHandle m_OceanJobHandle = default(JobHandle);
    JobHandle m_NormalsJobHndl = default(JobHandle);
    MeshFilter m_MeshFilter;
    Mesh m_Mesh;

    protected void Start()
    {
        m_MeshFilter = gameObject.GetComponent<MeshFilter>();
        m_Mesh = m_MeshFilter.mesh;
        m_Mesh.MarkDynamic();

        // this persistent memory setup assumes our vertex count will not expand
        m_Vertices = new NativeArray<Vector3>[2];
        m_Vertices[READ] = new NativeArray<Vector3>(m_Mesh.vertices, Allocator.Persistent);
        m_Vertices[WRITE] = new NativeArray<Vector3>(m_Mesh.vertices, Allocator.Persistent);

        m_Normals = new NativeArray<Vector3>(m_Mesh.normals, Allocator.Persistent);
        m_Indices = new NativeArray<int>(m_Mesh.triangles, Allocator.Persistent);
        m_TriNormals = new NativeArray<Vector3>(m_Mesh.triangles.Length / 3, Allocator.Persistent);

        m_VerticesToTrianglesMapping = new NativeArray<int>(m_Mesh.vertexCount * 6, Allocator.Persistent);
        m_VerticesToTrianglesMappingCount = new NativeArray<int>(m_Mesh.vertexCount, Allocator.Persistent);
        for (int i = 0; i < m_VerticesToTrianglesMapping.Length; i++)
            m_VerticesToTrianglesMapping[i] = -1;


        for (int triId = 0; triId < m_Indices.Length/3; triId++)
        {
            int vId0 = m_Indices[triId * 3 + 0];
            int vId1 = m_Indices[triId * 3 + 1];
            int vId2 = m_Indices[triId * 3 + 2];

            int vc0 = m_VerticesToTrianglesMappingCount[vId0];
            int vc1 = m_VerticesToTrianglesMappingCount[vId1];
            int vc2 = m_VerticesToTrianglesMappingCount[vId2];

            m_VerticesToTrianglesMapping[vId0 * 6 + vc0++] = triId;
            m_VerticesToTrianglesMapping[vId1 * 6 + vc1++] = triId;
            m_VerticesToTrianglesMapping[vId2 * 6 + vc2++] = triId;

            m_VerticesToTrianglesMappingCount[vId0] = vc0;
            m_VerticesToTrianglesMappingCount[vId1] = vc1;
            m_VerticesToTrianglesMappingCount[vId2] = vc2;
        }


        m_ModifiedVertices = new Vector3[m_Mesh.vertexCount];
        m_ModifiedNormals = new Vector3[m_Mesh.vertexCount];
    }

    [BurstCompile]
    struct OceanJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> verticesIn;

        [WriteOnly]
        public NativeArray<Vector3> verticesOut;

        public float time;
        public float perlinStrength;
        public float rippleStrength;
        public float scale;

        public void Execute(int i)
        {
            Vector3 vertex = verticesIn[i];

            //ripple
            Vector2 offset = new Vector2(vertex.x, vertex.z);
            float ripple = Mathf.Sin(time + offset.magnitude);

            //perlin
            float perlin = Mathf.PerlinNoise((time + vertex.x) * scale, (time + vertex.z) * scale);
            vertex.y = perlin * perlinStrength + ripple * rippleStrength;

            verticesOut[i] = vertex;
        }
    }

    [BurstCompile]
    struct RecalculateNormalsJob : IJob
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;

        [ReadOnly]
        public NativeArray<int> indices;

       // [WriteOnly]
        public NativeArray<Vector3> normals;


        public void Execute()
        {
            //clean normals
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = new Vector3();
            }

            //compute triangle normal and add it to its vertices
            for (int i = 0; i < indices.Length / 3; i++)
            {
                int pId1 = indices[i * 3 + 0];
                int pId2 = indices[i * 3 + 1];
                int pId3 = indices[i * 3 + 2];

                Vector3 p1 = vertices[pId1];
                Vector3 p2 = vertices[pId2];
                Vector3 p3 = vertices[pId3];

                //compute triangle normals
                Vector3 v1 = p2 - p1;
                Vector3 v2 = p3 - p1;
                Vector3 normal = Vector3.Cross(v1, v2);

                normal = math.normalize(normal);

                //scatter to vertices
                normals[pId1] += normal;
                normals[pId2] += normal;
                normals[pId3] += normal;
            }

            //go thru vertices and normalize
            for (int i = 0; i < vertices.Length; ++i)
            {
                //normals[i] = Vector3.Normalize(normals[i]);
                normals[i] = math.normalize(normals[i]);
            }
        }
    }

    [BurstCompile]
    struct ComputeTrisNormalsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;

        [ReadOnly]
        public NativeArray<int> indices;

        [WriteOnly]
        public NativeArray<Vector3> triNormals;


        public void Execute(int i)
        {
            int pId1 = indices[i * 3 + 0];
            int pId2 = indices[i * 3 + 1];
            int pId3 = indices[i * 3 + 2];

            Vector3 p1 = vertices[pId1];
            Vector3 p2 = vertices[pId2];
            Vector3 p3 = vertices[pId3];

            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p1;

            Vector3 normal = Vector3.Cross(v1, v2);

            triNormals[i] = math.normalize(normal);   
        }
    }

    [BurstCompile]
    struct ComputeNormalsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> verticesToTrianglesMapping;

        [ReadOnly]
        public NativeArray<int> verticesToTrianglesMappingCount;

        [ReadOnly]
        public NativeArray<Vector3> triNormals;

        [WriteOnly]
        public NativeArray<Vector3> normals;

        public void Execute(int i)
        {
            Vector3 n = new Vector3();

            for (int j = 0; j < verticesToTrianglesMappingCount[i]; j++)
            {
                int triId = verticesToTrianglesMapping[i * 6 + j];
                n += triNormals[triId];
            }
            
            normals[i] = math.normalize(n);
            
        }
    }




    [BurstCompile]
    struct ComputeTrisNormalsJobSimd : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;

        [ReadOnly]
        public NativeArray<int> indices;

        [WriteOnly]
        public NativeArray<Vector3> triNormals;


        public void Execute(int i)
        {
            int pId1 = indices[i * 3 + 0];
            int pId2 = indices[i * 3 + 1];
            int pId3 = indices[i * 3 + 2];

            float3 p1 = vertices[pId1];
            float3 p2 = vertices[pId2];
            float3 p3 = vertices[pId3];

            float3 v1 = p2 - p1;
            float3 v2 = p3 - p1;

            float3 normal = math.cross(v1, v2);

            triNormals[i] = math.normalize(normal);
        }
    }

    [BurstCompile]
    struct ComputeNormalsJobSimd : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> verticesToTrianglesMapping;

        [ReadOnly]
        public NativeArray<int> verticesToTrianglesMappingCount;

        [ReadOnly]
        public NativeArray<Vector3> triNormals;

        [WriteOnly]
        public NativeArray<Vector3> normals;

        public void Execute(int i)
        {
            float3 n = new float3();
            int vc = verticesToTrianglesMappingCount[i];
            for (int j = 0; j < vc; j++)
            {
                int triId = verticesToTrianglesMapping[i * 6 + j];
                float3 triN= triNormals[triId];
                n += triN;
            }

            //normals[i] = math.normalize(n);
            normals[i] = n / vc;

        }
    }




    [BurstCompile]
    struct RecalculateNormalsJobSimdOptimized : IJob
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices;

        [ReadOnly]
        public NativeArray<int> indices;

        // [WriteOnly]
        public NativeArray<Vector3> normals;


        public void Execute()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = new float3();
            }

            for (int i = 0; i < indices.Length / 3; i++)
            {
                int pId1 = indices[i * 3 + 0];
                int pId2 = indices[i * 3 + 1];
                int pId3 = indices[i * 3 + 2];

                float3 p1f3 = vertices[pId1];
                float3 p2f3 = vertices[pId2];
                float3 p3f3 = vertices[pId3];

                //float4 p1 = new float4(p1f3, 0);
                //float4 p2 = new float4(p2f3, 0);
                //float4 p3 = new float4(p3f3, 0);

                //float4 v1 = p2 - p1;
                //float4 v2 = p3 - p1;

                float3 v1 = p2f3 - p1f3;
                float3 v2 = p3f3 - p1f3;
                float3 normal = math.cross(v1.xyz, v2.xyz);

                normal = math.normalize(normal);

                float3 n1 = normals[pId1];
                float3 n2 = normals[pId2];
                float3 n3 = normals[pId3];

                normals[pId1] = n1 + normal;
                normals[pId2] = n2 + normal;
                normals[pId3] = n3 + normal;
            }

            for (int i = 0; i < vertices.Length; ++i)
            {
                //normals[i] = Vector3.Normalize(normals[i]);
                normals[i] = math.normalize(normals[i]);
            }
        }
    }




    //1. COMPUTE OVER FRAME
    //2. NORMAL CALCULATIONS
    //3. SIMD STUFF ComputeTrisNormalsJob
    //4. https://forum.unity.com/threads/burst-simd-and-float3-float4-best-practices.527504/
    public void Update()
    {
        //DO CALCULATION OVER THE FRAME LENGTH and RENDERING
        Profiler.BeginSample("__WAIT_FOR_JOB_COMPLETE__");
        m_OceanJobHandle.Complete();
        Swap<NativeArray<Vector3>>(m_Vertices);
        Profiler.EndSample();

        Profiler.BeginSample("__SCHEDULE_JOBS_ASYNC__");
        var oceanJob = new OceanJob()
        {
            verticesIn = m_Vertices[READ],
            verticesOut = m_Vertices[WRITE],

            time = Time.time * m_TimeMult,
            scale = m_Scale,

            perlinStrength = m_PerlinStrength,
            rippleStrength = m_RippleStrength,
        };

        m_OceanJobHandle = oceanJob.Schedule(m_Mesh.vertexCount, 64);

        //limit number of threads to two
        //m_OceanJobHandle = oceanJob.Schedule(m_Mesh.vertexCount, m_Mesh.vertexCount / 2);


        //var recalculateNormalsJob = new RecalculateNormalsJob
        //{
        //    vertices = m_Vertices[READ],
        //    normals = m_Normals,
        //    indices = m_Indices
        //};
        //m_NormalsJobHndl = recalculateNormalsJob.Schedule();

        var triNormalsJob = new ComputeTrisNormalsJobSimd
        {
            vertices = m_Vertices[READ],
            triNormals = m_TriNormals,
            indices = m_Indices,
        };
        m_NormalsJobHndl = triNormalsJob.Schedule(m_Indices.Length / 3, 64);

        var normalsJob = new ComputeNormalsJobSimd
        {
            verticesToTrianglesMapping = m_VerticesToTrianglesMapping,
            verticesToTrianglesMappingCount = m_VerticesToTrianglesMappingCount,
            triNormals = m_TriNormals,
            normals = m_Normals
        };
        m_NormalsJobHndl = normalsJob.Schedule(m_Mesh.vertexCount, 64, m_NormalsJobHndl);

        //m_NormalsJobHndl.Complete();


        //jobs wont be scheduled until below function is called or something starts waiting for them
        JobHandle.ScheduleBatchedJobs();
        Profiler.EndSample();

        

    }

    public void LateUpdate()
    {
        Profiler.BeginSample("__COPY_VERTICES__");
        //m_Vertices[READ].CopyTo(m_ModifiedVertices);
      //  IntPtr ptr = new IntPtr(m_Vertices[READ].GetUnsafePtr());
        var gch = GCHandle.Alloc(m_ModifiedVertices, GCHandleType.Pinned);
        UnsafeUtility.MemCpy(gch.AddrOfPinnedObject().ToPointer(), m_Vertices[READ].GetUnsafeReadOnlyPtr(), m_ModifiedVertices.Length*4*3);
        gch.Free();
        Profiler.EndSample();


        Profiler.BeginSample("__RECALCULATE_NORMALS__");
        //m_Mesh.RecalculateNormals();
        //var recalculateNormalsJob = new RecalculateNormalsJobSimdOptimized
        //{
        //    vertices = m_Vertices[READ],
        //    normals = m_Normals,
        //    indices = m_Indices
        //};
        

        //JobHandle normalsJobHndl = recalculateNormalsJob.Schedule();
        //normalsJobHndl.Complete();
        //Profiler.EndSample();
        m_NormalsJobHndl.Complete();
        Profiler.EndSample();

        Profiler.BeginSample("__COPY_NORMALS__");
        var nch = GCHandle.Alloc(m_ModifiedNormals, GCHandleType.Pinned);
        UnsafeUtility.MemCpy(nch.AddrOfPinnedObject().ToPointer(), m_Normals.GetUnsafePtr(), m_ModifiedNormals.Length * 4 * 3);
        nch.Free();
        Profiler.EndSample();

        m_Mesh.vertices = m_ModifiedVertices;
        m_Mesh.normals = m_ModifiedNormals;
     
    }

    public static void Swap<T>(T[] array)
    {
        T tmp = array[0];
        array[0] = array[1];
        array[1] = tmp;
    }

    private void OnDestroy()
    {
        m_OceanJobHandle.Complete();
        if (m_Normals.IsCreated)
        {
            m_Vertices[READ].Dispose();
            m_Vertices[WRITE].Dispose();
            m_Normals.Dispose();
            m_TriNormals.Dispose();
            m_VerticesToTrianglesMapping.Dispose();
            m_VerticesToTrianglesMappingCount.Dispose();
            m_Indices.Dispose();
        }
    }
}
