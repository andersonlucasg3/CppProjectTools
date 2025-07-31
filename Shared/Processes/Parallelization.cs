namespace Shared.Processes;

public static class Parallelization
{
    public static void ForEach<T>(T[] InSourceArray, Action<T> Action)
    {
        if (InSourceArray.Length == 0) return;

        ThreadWorker.ExecuteOnExclusiveThread(Info =>
        {
            foreach (T Element in InSourceArray)
            {
                ThreadWorker.Execute(() =>
                {
                    Action.Invoke(Element);

                    Info?.Release();
                });
            }

            Info?.Wait(InSourceArray.Length);
        });
    }
}