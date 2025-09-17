using System.Runtime.InteropServices;

namespace ProjectTools.IO;

public abstract class TFileSystemReference<T>
    where T : TFileSystemReference<T>
{
    protected static Func<string, T>? FactoryFunc;

    private static readonly Dictionary<string, T> AllReferences = [];

    public readonly string PlatformPath;
    public readonly string PlatformRelativePath;

    public readonly string FullPath;
    public readonly string RelativePath;

    public readonly string Name;

    public bool bExists { get; protected set; } = false;

    public T Combine(params string[] InPathComponents)
    {
        return Get(Path.Combine([PlatformPath, .. InPathComponents]));
    }

    public abstract void Delete();

    public override string ToString()
    {
        return FullPath;
    }

    protected TFileSystemReference(string InPath)
    {
        PlatformPath = Path.GetFullPath(GetPlatformPath(InPath));
        PlatformRelativePath = Path.GetRelativePath(Environment.CurrentDirectory, PlatformPath);

        FullPath = GetUniversalPath(PlatformPath);

        string CurrentDirectoryUniversal = GetUniversalPath(Environment.CurrentDirectory);

        if (FullPath == CurrentDirectoryUniversal)
        {
            RelativePath = ".";
        }
        else if (FullPath.StartsWith(CurrentDirectoryUniversal))
        {
            RelativePath = FullPath.Replace(CurrentDirectoryUniversal, "")[1..];
        }
        else
        {
            RelativePath = FullPath;
        }

        Name = GetName(PlatformPath) ?? PlatformPath;
    }

    public abstract void UpdateExistance();

    protected abstract string? GetName(string InPath);

    protected static T Get(string InPath)
    {
        if (string.IsNullOrEmpty(Path.GetPathRoot(InPath)))
        {
            InPath = Path.Combine(Environment.CurrentDirectory, InPath);
        }

        lock (AllReferences)
        {
            if (AllReferences.TryGetValue(InPath, out T? Reference))
            {
                return Reference;
            }

            T NewReference = FactoryFunc!.Invoke(InPath);

            AllReferences.Add(InPath, NewReference);
            return NewReference;
        }
    }

    private static string GetPlatformPath(string InFile)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (InFile.StartsWith('/'))
            {
                return InFile[1..].Insert(1, ":").Replace("/", "\\");
            }
        }

        return InFile;
    }

    private static string GetUniversalPath(string InFile)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (InFile.Contains(":\\"))
            {
                return InFile.Remove(1, 1).Insert(0, "/").Replace('\\', '/');
            }
        }
        return InFile;
    }
}
