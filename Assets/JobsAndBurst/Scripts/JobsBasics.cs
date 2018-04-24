using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


/// <summary>
/// Some basics how to use the job system and a couple of patterns how to schedule them in a different ways from FOR loops.
/// Just uncomment the subsequent lines in the Update loop and observe the effect in the Profiler in Timeline view.
/// Some methods intentionally won't work. The fix should be in the commented code.
/// More examples can be found here https://github.com/stella3d/job-system-cookbook
/// </summary>
public class JobsBasics : MonoBehaviour
{
    
    struct PowJob : IJob
    {
        public int dataSize;

        //[ReadOnly]
        public NativeArray<float> dataIn;

        //[WriteOnly]
        //[NativeDisableContainerSafetyRestriction]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float res = Mathf.Pow(dataIn[i], 2.0f);
                dataOut[i] = res;
            }

        }
    }



    struct PowParallelForJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> dataIn;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute(int i)
        {
            float res = Mathf.Pow(dataIn[i], 2.0f);
            dataOut[i] = res;
        }
    }


    struct PowParallelForInvJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<float> dataIn;

        [WriteOnly]
        //[NativeDisableParallelForRestriction]
        public NativeArray<float> dataOut;


        public void Execute(int i)
        {
            float res = Mathf.Pow(dataIn[i], 2.0f);
            dataOut[dataOut.Length - i -1] = res;

            //other way around
            //float res = Mathf.Pow(dataIn[dataIn.Length - i - 1], 2.0f);
            //dataOut[i] = res;
        }
    }

   

    public int dataSize = 100000;
    [Range(0,10)]
    public int forLoopSchedulesCount = 10;

    private NativeArray<float> dataA;
    private NativeArray<float> dataB;
    private NativeArray<float> dataC;


    private NativeArray<float>[] dataIns;
    private NativeArray<float>[] dataOuts;

    private JobHandle _jobHndl = default(JobHandle);

    // Use this for initialization
    void Start ()

    {
        dataA = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataB = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataC = new NativeArray<float>(dataSize, Allocator.Persistent);

        for (int i = 0; i < dataSize; i++)
        {
            dataA[i] = Random.value;
            dataB[i] = Random.value;
            dataC[i] = Random.value;
        }

        dataIns = new NativeArray<float>[10];
        dataOuts = new NativeArray<float>[10];
        for (int i = 0; i < 10; i++)
        {
            int size = dataSize / (i + 1);
            dataIns[i] = new NativeArray<float>(size, Allocator.Persistent);
            dataOuts[i] = new NativeArray<float>(size, Allocator.Persistent);
            
            for (int j = 0; j < size; j++)
            {
                dataA[j] = Random.value;
                dataB[j] = Random.value;
                dataC[j] = Random.value;
            }
            Debug.Log("DataSize " + i + ": " + size);
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        UnityEngine.Profiling.Profiler.BeginSample("__ARRAY_OPS__");

        JobsScheduling();
        //SafetySystem();
        //ParallelFor();
        //ParallelForInv();
        //ScheduleSequentialInForLoop();
        //ScheduleParallelInForLoop();
        //ScheduleParallelForInParallelInForLoop();
        //ScheduleBatchedJobs();

        //DoubleBuffering - see corresponding script

        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void JobsScheduling()
    {
     
        var powJob = new PowJob
        {
            dataSize = dataSize,
            dataIn = dataA,
            dataOut = dataB
        };

        //SINGLE THREADED
        // powJob.Run();

        JobHandle jobHndl = powJob.Schedule();
        jobHndl.Complete();
    }

    private void SafetySystem()
    {
        //1. DEPENDENCIES
        //2. COMBINE DEPENDENCIES
        //2. READING FROM THE SAME ARRAY IS OK [Read/WriteOnly] 
        //2. SAFETY OVERRIDE

        var powJobA = new PowJob
        {
            dataSize = dataSize,
            dataIn = dataA,
            dataOut = dataB
        };
        JobHandle jobHndlA = powJobA.Schedule();

        var powJobB = new PowJob
        {
            dataSize = dataSize,
            dataIn = dataB,
            //dataIn = dataA, //also uncomment [READ/WRITE] attributes in the PowJob
            dataOut = dataC
        };
        JobHandle jobHndlB = powJobB.Schedule();

        //Dependecy
        //JobHandle jobHndlB = powJobB.Schedule(jobHndlA);

        JobHandle jobHndl = JobHandle.CombineDependencies(jobHndlA, jobHndlB);
        jobHndl.Complete();


    }

    private void ParallelFor()
    {
        var powJob = new PowParallelForJob
        {
            dataIn = dataA,
            dataOut = dataB
        };

        JobHandle jobHndl = powJob.Schedule(dataSize, 64);

        jobHndl.Complete();
    }

    private void ParallelForInv()
    {
        //1. DISABLE SAFETY CHECK - [NativeDisableContainerSafetyRestriction]
        //2. READ INVERTED

        //https://forum.unity.com/threads/how-to-write-to-another-index-with-the-jobsystem.519981/
        //As a general rule aiming for determinism is a good idea when writing multithreaded code.
        //And in this case even if you use atomic ops it would still result in non-deterministic behaviour, 
        //since it depends on which jobs runs where.Double buffering as M_R explains is a solid solution to that.

        //Secondly using atomic ops or generally reading / writing to the same array that another worker thread might be reading/writing to will most likely result in significantly worse performance.

        //Specifically when reading / writeing to the same cache line in parallel the CPU has to perform very expensive cache line synchronization stalling the whole pipeline.

        //These are two big reasons why by default we allow only writing to the parallel for index.
        var powInvJob = new PowParallelForInvJob
        {
            dataIn = dataA,
            dataOut = dataB
        };

        JobHandle jobHndl = powInvJob.Schedule(dataSize, 64);

        jobHndl.Complete();
    }

    private void ScheduleSequentialInForLoop()
    {

        JobHandle jobHandle = default(JobHandle);
        for (int i = 0; i < forLoopSchedulesCount; i++)
        {
            var powJob = new PowJob
            {
                dataSize = dataIns[i].Length,
                dataIn = dataIns[i],
                dataOut = dataOuts[i]
            };
            jobHandle = powJob.Schedule(jobHandle);
        }
        jobHandle.Complete();

    }

    private void ScheduleParallelInForLoop()
    {

        JobHandle jobHandle = default(JobHandle);
        NativeArray<JobHandle> parallelJobHandles = new NativeArray<JobHandle>(forLoopSchedulesCount, Allocator.Temp);
        for (int i = 0; i < forLoopSchedulesCount; i++)
        {
            var powJob = new PowJob
            {
                dataSize = dataIns[i].Length,
                dataIn = dataIns[i],
                dataOut = dataOuts[i]
            };
            parallelJobHandles[i] = powJob.Schedule(jobHandle);
        }

        jobHandle = JobHandle.CombineDependencies(parallelJobHandles);
        parallelJobHandles.Dispose();

        jobHandle.Complete();

    }

    private void ScheduleParallelForInParallelInForLoop()
    {

        JobHandle jobHandle = default(JobHandle);
        NativeArray<JobHandle> parallelJobHandles = new NativeArray<JobHandle>(forLoopSchedulesCount, Allocator.Temp);
        for (int i = 0; i < forLoopSchedulesCount; i++)
        {
            var powJob = new PowParallelForJob
            {
      
                dataIn = dataIns[i],
                dataOut = dataOuts[i]
            };
            parallelJobHandles[i] = powJob.Schedule(dataIns[i].Length, 64, jobHandle);
        }

        jobHandle = JobHandle.CombineDependencies(parallelJobHandles);
        parallelJobHandles.Dispose();

        jobHandle.Complete();

    }

    private void ScheduleBatchedJobs()
    {
        var powJob = new PowParallelForJob
        {
            dataIn = dataA,
            dataOut = dataB
        };


        //REMEMBA TO TURN ON THE FAKE UPDATE
        //SCHEDULE EARLY
        _jobHndl = powJob.Schedule(dataSize, 64);

        //THIS ACTUALLY MAKES JOBS AVAILABLE FOR THE EXECUTION
        JobHandle.ScheduleBatchedJobs();

    }

    private void LateUpdate()
    {
        UnityEngine.Profiling.Profiler.BeginSample("__WAIT_FOR_COMPLETION__");
        //COMPLETE LATE
        _jobHndl.Complete();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    private void OnDestroy()
    {
        dataA.Dispose();
        dataB.Dispose();
        dataC.Dispose();

        for (int i = 0; i < 10; i++)
        {
            dataIns[i].Dispose();
            dataOuts[i].Dispose();
        }
    }
}
