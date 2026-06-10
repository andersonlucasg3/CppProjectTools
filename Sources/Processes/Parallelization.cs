namespace ProjectTools.Processes;

public static class Parallelization
{
    /// <summary>
    /// Executes <paramref name="Action"/> for every element in <paramref name="InSourceArray"/>
    /// using the shared ThreadWorker pool, then blocks until all items complete.
    /// The calling thread helps process work from the shared queue while waiting,
    /// so nested ForEach calls do not cause thread exhaustion.
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
        while (!BatchInfo.IsComplete())
        {
            if (!ThreadWorker.TryExecutePendingAction())
            {
                // Nothing in the queue — yield to let workers produce more work
                Thread.Sleep(1);
            }
        }
    }
}