using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;

public class FakeLongUpdate : MonoBehaviour {

    public int sleepMs = 10;


	void Update () {
        Profiler.BeginSample("__FAKE_LONG_UPDATE__");
        Thread.Sleep(sleepMs);
        Profiler.EndSample();
	}
}
