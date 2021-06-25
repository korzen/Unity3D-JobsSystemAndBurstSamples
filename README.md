# Unity3D-JobsSystemAndBurstSamples

Last checked with Unity 2020.3.12

JobBasics - Shows how to use the job system and a couple of patterns how to schedule them in a different ways from FOR loops.
Just uncomment the subsequent lines JobBasics.cs in the Update loop and observe the effect in the Profiler in Timeline view.
Some methods intentionally won't work. The fix should be in the commented code.

Ocean - A simplest possible parallelization of the ocean simulation using ParallelForJob to modify the mesh vertices. Check a "multithreaded" box to enable jobs and burst

OceanDoubleBuffered - An optimized ocean simulation using double buffering to overlap vertex modification with normals calculations

More examples can be found here:
https://github.com/stella3d/job-system-cookbook
https://github.com/ErikMoczi/Unity.TestRunner.Jobs
