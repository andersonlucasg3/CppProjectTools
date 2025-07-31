using Shared.Exceptions;

namespace Shared.IO;

public class DirectoryReference : TFileSystemReference<DirectoryReference>
{
    public static void Initialize()
    {
        FactoryFunc = Path => new(Path);
    }

    public void Create()
    {
        lock (this)
        {
            Directory.CreateDirectory(PlatformPath);

            UpdateExistance();
        }
    }

    public void Delete(bool bRecursive)
    {
        lock (this)
        {
            if (bExists) Directory.Delete(PlatformPath, bRecursive);

            UpdateExistance();
        }
    }

    public DirectoryReference? GetParent()
    {
        DirectoryInfo Info = new(PlatformPath);

        if (Info.Parent is null) return null;

        return Get(Info.Parent.FullName);
    }

    public FileReference CombineFile(params string[] InPathComponents)
    {
        string FilePath = Path.Combine([PlatformPath, .. InPathComponents]);

        return FileReference.Get(FilePath);
    }

    public FileReference[] EnumerateFiles(string InSearchPattern, SearchOption InSearchOption = SearchOption.TopDirectoryOnly)
    {
        lock (this)
        {
            if (!bExists) return [];

            return [.. Directory.EnumerateFiles(PlatformPath, InSearchPattern, InSearchOption).Select(FileReference.Get)];
        }
    }

    public DirectoryReference[] EnumerateDirectories(string InSearchPattern, SearchOption InSearchOption = SearchOption.TopDirectoryOnly)
    {
        lock (this)
        {
            if (!bExists) return [];
            
            return [.. Directory.EnumerateDirectories(PlatformPath, InSearchPattern, InSearchOption).Select(Get)];
        }
    }

    public string[] PathComponents()
    {
        return FullPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    public string[] RelativePathComponents(DirectoryReference? InRelativeToDirectory = null)
    {
        string RelativePathToUse = RelativePath;

        if (InRelativeToDirectory is not null)
        {
            RelativePathToUse = RelativePath.Replace(InRelativeToDirectory.RelativePath, "");
        }

        return RelativePathToUse.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    public override void Delete()
    {
        Delete(false);
    }

    protected DirectoryReference(string InPath) : base(InPath)
    {
        UpdateExistance();
    }

    public override void UpdateExistance()
    {
        bExists = Directory.Exists(PlatformPath);
    }

    public static implicit operator DirectoryReference(string InPath)
    {
        return Get(InPath);
    }

    public static implicit operator DirectoryReference(string[] InPathComponents)
    {
        return Get(Path.Combine(InPathComponents));
    }

    protected override string? GetName(string InPath)
    {
        return new DirectoryInfo(InPath).Name;
    }
}

public class MissingFileExtensionException(string InMessage) : ABaseException(InMessage);
