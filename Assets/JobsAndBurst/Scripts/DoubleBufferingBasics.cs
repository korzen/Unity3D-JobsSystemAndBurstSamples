using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// An example to show simple double buffering strategy.
/// The AddJob can use results from the previous PowJob calculation while PowJob computes the new results.
/// See OceanDoubleBuffered for more complex example.
/// </summary>
public class DoubleBufferingBasics : MonoBehaviour {


    struct PowJob : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataIn;

        [WriteOnly]
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

    struct AddJob : IJob
    {
        public int dataSize;

        [ReadOnly]
        public NativeArray<float> dataIn;

        [WriteOnly]
        public NativeArray<float> dataOut;

        public void Execute()
        {
            for (int i = 0; i < dataSize; i++)
            {
                float res = dataIn[i] + 10;
                dataOut[i] = res;
            }
        }
    }


    public int dataSize = 100000;

    private const int READ = 0;
    private const int WRITE = 1;

    
    private NativeArray<float>[] dataA;
    private NativeArray<float>[] dataB;
    private NativeArray<float>[] dataC;

    private JobHandle powJobHndl;
    private JobHandle addJobHndl;

    // Use this for initialization
    void Start ()
    {
        dataA = new NativeArray<float>[2];
        dataB = new NativeArray<float>[2];
        dataC = new NativeArray<float>[2];

        dataA[READ] = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataB[READ] = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataC[READ] = new NativeArray<float>(dataSize, Allocator.Persistent);

        dataA[WRITE] = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataB[WRITE] = new NativeArray<float>(dataSize, Allocator.Persistent);
        dataC[WRITE] = new NativeArray<float>(dataSize, Allocator.Persistent);
    }
	
	// Update is called once per frame
	void Update ()
    {

        var powJob = new PowJob
        {
            dataSize = dataSize,
            dataIn = dataA[READ],
            dataOut = dataB[WRITE]
        };
        powJobHndl = powJob.Schedule();


        var addJob = new AddJob
        {
            dataSize = dataSize,
            dataIn = dataB[READ],
            dataOut = dataC[WRITE]
        };

        addJobHndl = addJob.Schedule();

        JobHandle.ScheduleBatchedJobs();
    }

    private void LateUpdate()
    {
        powJobHndl.Complete();
        addJobHndl.Complete();

        Swap<NativeArray<float>>(dataA);
        Swap<NativeArray<float>>(dataB);
        Swap<NativeArray<float>>(dataC);
    }

    private void Swap<T>(T[] array)
    {
        T tmp = array[0];
        array[0] = array[1];
        array[1] = tmp;
    }

    private void OnDestroy()
    {
        dataA[READ].Dispose();
        dataB[READ].Dispose();
        dataC[READ].Dispose();

        dataA[WRITE].Dispose();
        dataB[WRITE].Dispose();
        dataC[WRITE].Dispose();
    }
}
