namespace ProjectTools.Processes;

public class ThreadInfo(Thread? InThread, bool InTemporary = false)
{
    private readonly Lock _lock = new();
    private int _count = 0;

    public readonly Thread? Thread = InThread;
    public bool bTemporary => InTemporary;

    public ThreadInfo() : this(null)
    {
        //
    }

    ~ThreadInfo()
    {
        Release();
    }

    public void Add(int InCount)
    {
         lock (_lock)
        {
            _count += InCount;
        }
    }

    public void Wait(int InWaitCount = 1)
    {
        Add(InWaitCount);

        int Count;
        do
        {
            Thread.Sleep(1);

            lock (_lock)
            {
                Count = _count;
            }
        }
        while (Count > 0);
    }

    public void Release()
    {
        lock (_lock)
        {
            _count -= 1;
        }
    }
}

interface IActionInfo
{
    public void Invoke();
}

class ActionInfo : IActionInfo
{
    public required Action Action;

    public void Invoke()
    {
        Action.Invoke();
    }
}

class ActionInfo<T> : IActionInfo
{
    public required Action<T> Action;
    public required T Object;

    public void Invoke()
    {
        Action.Invoke(Object);
    }
}

public static class ThreadWorker
{
    private static readonly Lock _threadLock = new();
    private static readonly Thread _mainWorkerThread = new(ThreadWorkerRunner)
    {
        Name = "MainThreadWorker",
        IsBackground = true,
    };

    private static readonly Queue<ThreadInfo> _availableThreads = [];
    private static readonly Dictionary<Thread, ThreadInfo> _threadInfoMap = [];

    private static readonly Queue<IActionInfo> _actionQueue = [];

    private static bool _shouldKeepRunning = true;
    private static int _preallocatedThreadCount = 1;

    private static bool _bIsSingleThreaded;

    public static void SingleThreaded()
    {
        _bIsSingleThreaded = true;
    }

    public static void PreallocateThreads(int InThreadCount)
    {
        _preallocatedThreadCount = Math.Max(1, InThreadCount);

        _mainWorkerThread.Start();

        int Count = Math.Max(1, InThreadCount);
        for (int Index = 0; Index < Count; Index++)
        {
            CreateNewWorkerThread();
        }
    }

    public static void Terminate()
    {
        if (_bIsSingleThreaded)
        {
            return;
        }

        ThreadInfo[] ThreadInfos;
        lock (_threadLock)
        {
            _shouldKeepRunning = false;

            ThreadInfos = [.. _threadInfoMap.Values];
        }

        foreach (ThreadInfo ThreadInfo in ThreadInfos)
        {
            ThreadInfo.Release();
            ThreadInfo.Thread?.Join();
        }

        _mainWorkerThread.Join();
    }

    public static void Execute(Action Action)
    {
        if (_bIsSingleThreaded)
        {
            Action.Invoke();
            return;
        }

        lock (_threadLock)
        {
            _actionQueue.Enqueue(new ActionInfo()
            {
                Action = Action
            });

            if (_threadInfoMap.Count >= _preallocatedThreadCount)
            {
                // create and let it work
                CreateNewWorkerThread(false).Release();
            }
        }
    }

    public static void Execute<T>(Action<T> InAction, T InObject)
    {
        if (_bIsSingleThreaded)
        {
            InAction.Invoke(InObject);
            return;
        }

        lock (_threadLock)
        {
            _actionQueue.Enqueue(new ActionInfo<T>()
            {
                Action = InAction,
                Object = InObject
            });

            if (_threadInfoMap.Count >= _preallocatedThreadCount)
            {
                // create and let it work
                CreateNewWorkerThread(false).Release();
            }
        }
    }

    public static void ExecuteOnExclusiveThread(Action<ThreadInfo?> InAction)
    {
        if (_bIsSingleThreaded)
        {
            InAction.Invoke(null);
            return;
        }

        Action<object?> ThreadAction = Obj =>
        {
            ThreadInfo? Info = Obj as ThreadInfo;
            InAction.Invoke(Info);
        };

        ThreadInfo ExecuteOnceInfo = new(new Thread(ThreadAction.Invoke)
        {
            Name = "ThreadOnce",
            IsBackground = true,
        });

        ExecuteOnceInfo.Thread?.Start(ExecuteOnceInfo);
        ExecuteOnceInfo.Thread?.Join();
    }

    private static ThreadInfo CreateNewWorkerThread(bool bTemporary = false)
    {
        lock (_threadLock)
        {
            ThreadInfo NewThreadInfo = new(new(ThreadRunner)
            {
                Name = "ThreadWorker",
                IsBackground = true,
            }, bTemporary);

            if (!bTemporary)
            {
                _availableThreads.Enqueue(NewThreadInfo);
            }

            _threadInfoMap.Add(NewThreadInfo.Thread!, NewThreadInfo);

            NewThreadInfo.Thread!.Start();

            return NewThreadInfo;
        }
    }

    private static void ThreadWorkerRunner()
    {
        bool bShouldKeepRunning;
        do
        {
            Thread.Sleep(1);

            bool bReleasedThread = false;

            lock (_threadLock)
            {
                if (_actionQueue.Count > 0 && _availableThreads.TryDequeue(out ThreadInfo? ThreadInfo))
                {
                    bReleasedThread = true;
                    ThreadInfo.Release();
                }

                bShouldKeepRunning = _shouldKeepRunning;
            }

            if (!bReleasedThread && _preallocatedThreadCount <= _actionQueue.Count)
            {
                // if no threads are available, just execute it
                ExecuteAction(false);
            }
        }
        while (bShouldKeepRunning);
    }

    private static void ThreadRunner()
    {
        bool bShouldKeepRunning = true;
        do
        {
            if (!_threadInfoMap.TryGetValue(Thread.CurrentThread, out ThreadInfo? Info))
            {
                Thread.Sleep(1);

                continue;
            }

            Info.Wait();

            ExecuteAction();

            if (Info.bTemporary)
            {
                return;
            }

            lock (_threadLock)
            {
                bShouldKeepRunning = _shouldKeepRunning;

                if (bShouldKeepRunning)
                {
                    _availableThreads.Enqueue(Info);
                }
            }
        }
        while (bShouldKeepRunning);
    }

    private static void ExecuteAction(bool bShouldLock = true)
    {
        IActionInfo? ActionInfo;
        if (bShouldLock) lock (_threadLock) _actionQueue.TryDequeue(out ActionInfo);
        else _actionQueue.TryDequeue(out ActionInfo);
        ActionInfo?.Invoke();
    }
}