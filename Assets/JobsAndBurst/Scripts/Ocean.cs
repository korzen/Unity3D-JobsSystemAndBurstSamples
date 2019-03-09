using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// A simplest possible parallelization of the ocean simulation using ParallelForJob to modify the mesh vertices.
/// Check a "multithreaded" box to enable jobs and burst
/// More examples can be found here https://github.com/stella3d/job-system-cookbook
/// </summary>
public class Ocean : MonoBehaviour
{
    public bool multiThreaded = false;

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


    NativeArray<Vector3> m_Vertices;
    Vector3[] m_ModifiedVertices;

    JobHandle m_JobHandle;

    MeshFilter m_MeshFilter;
    Mesh m_Mesh;

    protected void Start()
    {
        m_MeshFilter = gameObject.GetComponent<MeshFilter>();
        m_Mesh = m_MeshFilter.mesh;
        m_Mesh.MarkDynamic();


        m_Vertices = new NativeArray<Vector3>(m_Mesh.vertices, Allocator.Persistent);
        m_ModifiedVertices = new Vector3[m_Vertices.Length];
    }

    [BurstCompile]
    struct OceanJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        public float time;
        public float perlinStrength;
        public float rippleStrength;
        public float scale;

        public void Execute(int i)
        {
            Vector3 vertex = vertices[i];

            //ripple
            Vector2 offset = new Vector2(vertex.x, vertex.z);
            float ripple = Mathf.Sin(time + offset.magnitude);

            //perlin
            float perlin = Mathf.PerlinNoise((time + vertex.x) * scale, (time + vertex.z) * scale); //calls the native code
            //float perlin = PerlinNoise.cnoise(new float2((time + vertex.x) * scale, (time + vertex.z) * scale)); //burst version is actually slower
            vertex.y = perlin * perlinStrength + ripple * rippleStrength;

            vertices[i] = vertex;
        }
    }




    public void Update()
    {

        var oceanJob = new OceanJob()
        {
            vertices = m_Vertices,
            time = Time.time * m_TimeMult,
            scale = m_Scale,

            perlinStrength = m_PerlinStrength,
            rippleStrength = m_RippleStrength,
        };

        if (multiThreaded)
        {
            m_JobHandle = oceanJob.Schedule(m_Vertices.Length, 64);

            //jobs wont be scheduled unless explicitly invoked or something starts waiting for them
            JobHandle.ScheduleBatchedJobs();
        }
        else
        {
            Profiler.BeginSample("RUN");
            oceanJob.Run(m_Vertices.Length);
            Profiler.EndSample();
        }



    }

    public void LateUpdate()
    {
        Profiler.BeginSample("__WAIT_FOR_JOB_COMPLETE__");
        m_JobHandle.Complete();
        Profiler.EndSample();

        Profiler.BeginSample("__COPY_VERTICES__");
        m_Vertices.CopyTo(m_ModifiedVertices);
        Profiler.EndSample();


        m_Mesh.vertices = m_ModifiedVertices;

        Profiler.BeginSample("__RECALCULATE_NORMALS__");
        m_Mesh.RecalculateNormals();
        Profiler.EndSample();
    }

    private void OnDestroy()
    {
        if(m_Vertices.IsCreated)
            m_Vertices.Dispose();

    }
}