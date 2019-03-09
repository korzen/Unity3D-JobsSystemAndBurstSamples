using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Profiling;
using System.Threading;

/// <summary>
/// Just some test to compare the generated Assembly code in Jobs Inspector. See these for analysis:
/// https://forum.unity.com/threads/burst-simd-and-float3-float4-best-practices.527504/
/// https://forum.unity.com/threads/loop-vectorization-unroll.527355/
/// </summary>
public class SimdTest : MonoBehaviour
{
    [BurstCompile]
    struct SumJob : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                dataOut[i] = dataA[i] + dataB[i];
            }
        }
    }

    [BurstCompile]
    struct Float1Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float a = dataA[i];
                float b = dataB[i];
                float sum = a + b;
                float mul = a * b;
                float res = (sum - mul) / 10.0f;
                dataOut[i] = res;
            }
        }
    }

    [BurstCompile]
    struct Float3Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float3> dataA;

        [ReadOnly]
        public NativeArray<float3> dataB;

        [WriteOnly]
        public NativeArray<float3> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float3 a = dataA[i];
                float3 b = dataB[i];
                float3 sum = a + b;
                float3 mul = a * b;
                float3 res = (sum - mul) / 10.0f;
                dataOut[i] = res;
            }
        }
    }

    [BurstCompile]
    struct Float4to3Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float3 a = dataA[i].xyz;
                float3 b = dataB[i].xyz;
                float3 sum = a + b;
                float3 mul = a * b;
                float3 res = (sum - mul) / 10.0f;
                dataOut[i] = new float4(res, 0);
            }
        }
    }



    [BurstCompile]
    struct Float4Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;



        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float4 a = dataA[i];
                float4 b = dataB[i];
                float4 sum = a + b;
                float4 mul = a * b;
                float4 res = (sum - mul)/ 10.0f;
                dataOut[i] = res;
            }
        }
    }

[BurstCompile]
struct FloatAndFloat3Job : IJob
{
    public int dataSize;


    [ReadOnly]
    public NativeArray<float> dataS;

    [ReadOnly]
    public NativeArray<float3> dataA;

    [ReadOnly]
    public NativeArray<float3> dataB;

    [WriteOnly]
    public NativeArray<float3> dataOut;

    public void Execute()
    {
        for (int i = 0; i < dataSize; i++)
        {
            float3 a = dataA[i].xyz;
            float3 b = dataB[i].xyz;
            float s = dataS[i];
            float3 sum = a + b;
            float sumS = s + s;
            float3 mul = a * b;
            float3 res = (sum - mul) ;
            dataOut[i] = res * sumS;
        }
    }
}

    [BurstCompile]
    struct FloatAndFloat4Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataS;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float4 a = dataA[i];
                float4 b = dataB[i];
                float s = dataS[i];
                float4 sum = a + b;
                float sumS = s + s;
                float4 mul = a * b;
                float4 res = sum - mul;
                dataOut[i] = res / sumS;
            }
        }
    }

    [BurstCompile]
    struct FloatAndFloat4To3Job : IJob
    {
        public int dataSize;


        [ReadOnly]
        public NativeArray<float> dataS;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float3 a = dataA[i].xyz;
                float3 b = dataB[i].xyz;
                float  s = dataS[i];
                float3 sum = a + b;
                float sumS = s + s;
                float3 mul = a * b;
                float3 res = sum - mul;
                dataOut[i] = new float4(res, 0) * sumS;
            }
        }
    }

    [BurstCompile]
    struct SumFloat4Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                dataOut[i] = dataA[i] + dataB[i];
            }
        }
    }

    [BurstCompile]
    struct SumUnrollJob : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            //UNROLL
            for (int i = 0; i < dataSize; i+=4)
            {
                dataOut[i + 0] = dataA[i + 0] + dataB[i + 0];
                dataOut[i + 1] = dataA[i + 1] + dataB[i + 1];
                dataOut[i + 2] = dataA[i + 2] + dataB[i + 2];
                dataOut[i + 3] = dataA[i + 3] + dataB[i + 3];
            }
        }
    }

    [BurstCompile]
    struct SumUnroll2Job : IJob
    {
        public int dataSize;

        [ReadOnly] public NativeArray<float> dataA;

        [ReadOnly] public NativeArray<float> dataB;

        [WriteOnly] public NativeArray<float> dataOut;

        public void Execute()
        {
            //UNROLL
            for (int i = 0; i < dataSize; i+=4)
            {

                float4 a = new float4(dataA[i + 0], dataA[i + 1], dataA[i + 2], dataA[i + 3]);
                float4 b = new float4(dataB[i + 0], dataB[i + 1], dataB[i + 2], dataB[i + 3]);

                float4 sum = a + b;

                dataOut[i + 0] = sum.x;
                dataOut[i + 1] = sum.y;
                dataOut[i + 2] = sum.z;
                dataOut[i + 3] = sum.w;
            }
        }
    }

    [BurstCompile]
    struct SumUnroll3Job : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float4> dataA;

        [ReadOnly]
        public NativeArray<float4> dataB;

        [WriteOnly]
        public NativeArray<float4> dataOut;

        public void Execute()
        {
            //UNROLL
            for (int i = 0; i < dataSize; i ++)
            {
                float4 a =   dataA[i];
                float4 b =  dataB[i];

                float4 sum = a + b;

                dataOut[i] = sum;
            }
        }
    }

    [BurstCompile]
    struct SumParallelForJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute(int i)
        {
            dataOut[i] = dataA[i] + dataB[i];
        }
    }

    struct VerifyJob : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [ReadOnly]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                if (dataA[i] + dataB[i] != dataOut[i])
                    Debug.Log("Error at id " + i);
            }

        }
    }

    //  [BurstCompile]
    struct VerifyParallelForJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> dataA;

        [ReadOnly]
        public NativeArray<float> dataB;

        [ReadOnly]
        public NativeArray<float> dataOut;

        public void Execute(int i)
        {
            if (dataA[i] + dataB[i] != dataOut[i])
                Debug.Log("Error at id " +i);
        }
    }



    public int dataSize = 100000;


    private NativeArray<float> dataA;
    private NativeArray<float> dataB;
    private NativeArray<float> dataC;

    private NativeArray<float3> data3A;
    private NativeArray<float3> data3B;
    private NativeArray<float3> data3C;

    private NativeArray<float4> data4A;
    private NativeArray<float4> data4B;
    private NativeArray<float4> data4C;

    private JobHandle _jobHndl = default(JobHandle);

    // Use this for initialization
    void Start()
    {
        dataA = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataB = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataC = new NativeArray<float>(dataSize, Allocator.Persistent);

        data3A = new NativeArray<float3>(dataSize, Allocator.Persistent);
        data3B = new NativeArray<float3>(dataSize, Allocator.Persistent);
        data3C = new NativeArray<float3>(dataSize, Allocator.Persistent);


        data4A = new NativeArray<float4>(dataSize, Allocator.Persistent);
        data4B = new NativeArray<float4>(dataSize, Allocator.Persistent);
        data4C = new NativeArray<float4>(dataSize, Allocator.Persistent);

        for (int i = 0; i < dataSize; i++)
        {
            dataA[i] = UnityEngine.Random.value;
            dataB[i] = UnityEngine.Random.value;
            dataC[i] = UnityEngine.Random.value;

            data3A[i] = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            data3B[i] = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            data3C[i] = new float3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            data4A[i] = new float4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            data4B[i] = new float4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            data4C[i] = new float4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

     
    }

    private void Update()
    {
        Profiler.BeginSample("SUMMING");
        //var sumJob = new Float1Job
        //{
        //    dataSize = dataSize,
        //    dataA = dataA,
        //    dataB = dataB,
        //    dataOut = dataC
        //};

        //var sumJob = new Float3Job
        //{
        //    dataSize = dataSize,
        //    dataA = data3A,
        //    dataB = data3B,
        //    dataOut = data3C
        //};

        //var sumJob = new Float34Job
        //{
        //    dataSize = dataSize,
        //    dataA = data4A,
        //    dataB = data4B,
        //    dataOut = data4C
        //};

        var sumJob = new Float4Job
        {
            dataSize = dataSize,
            dataA = data4A,
            dataB = data4B,
            dataOut = data4C
        };

        //SINGLE THREADED
        sumJob.Run();

        //JobHandle jobHndl = sumJob.Schedule();
        //jobHndl.Complete();

        Profiler.EndSample();

        //var verifyJob = new VerifyParallelForJob
        //{

        //    dataA = dataA,
        //    dataB = dataB,
        //    dataOut = dataC
        //};
        //verifyJob.Schedule(dataSize, 64).Complete();
        

    }

    private void OnDestroy()
    {
        dataA.Dispose();
        dataB.Dispose();
        dataC.Dispose();

        data3A.Dispose();
        data3B.Dispose();
        data3C.Dispose();

        data4A.Dispose();
        data4B.Dispose();
        data4C.Dispose();
    }
}

