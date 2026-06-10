namespace ProjectTools.Processes;

public static class Parallelization
{
    /// <summary>
    /// Enqueues <paramref name="Action"/> for every element in <paramref name="InSourceArray"/>
    /// via the shared ThreadWorker pool, then blocks until all items complete.
    /// Worker threads and the MainThreadWorker dispatcher handle execution.
    /// The calling thread does NOT steal work — this prevents re-entrancy deadlocks
    /// when ForEach is called inside another ForEach action.
    /// </summary>
    public static void ForEach<T>(T[] InSourceArray, Action<T> Action)
    {
        if (InSourceArray.Length == 0) return;

        ThreadInfo BatchInfo = new();

        // Pre-increment counter BEFORE enqueuing — prevents race where
        // workers call Release() before Add() runs, causing underflow.
        BatchInfo.Add(InSourceArray.Length);

        // Enqueue all work items via the shared worker pool
        foreach (T Element in InSourceArray)
        {
            T Captured = Element;
            ThreadWorker.Execute(() =>
            {
                Action.Invoke(Captured);
                BatchInfo.Release();
            });
        }

        // Wait for our batch to complete. Worker threads and the dispatcher
        // handle execution. We do NOT steal work to avoid re-entrancy when
        // one ForEach action triggers a nested ForEach (e.g. module compile
        // triggering compile actions, then waiting for dependencies).
        while (!BatchInfo.IsComplete())
        {
            Thread.Sleep(1);
        }
    }
}