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

        // Help process work from the shared queue while our batch completes.
        // This replaces the old ExecuteOnExclusiveThread + Thread.Join() pattern
        // which created raw threads that blocked uselessly.
        BatchInfo.Add(InSourceArray.Length);
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