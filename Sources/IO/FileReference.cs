namespace ProjectTools.IO;

public class FileReference : TFileSystemReference<FileReference>
{
    public static void Initialize()
    {
        FactoryFunc = Path => new(Path);
    }

    public readonly DirectoryReference Directory;
    public readonly string NameWithoutExtension;
    public readonly string Extension;

    public void OpenRead(Action<FileStream> InAction)
    {
        lock (this)
        {
            using FileStream FileStream = File.OpenRead(PlatformPath);
            InAction.Invoke(FileStream);
            FileStream.Close();

            UpdateExistance();
        }
    }

    public void OpenWrite(Action<FileStream> InAction)
    {
        lock (this)
        {
            using FileStream FileStream = File.OpenWrite(PlatformPath);
            InAction.Invoke(FileStream);
            FileStream.Close();

            UpdateExistance();
        }
    }

    public void CopyTo(FileReference InDestination, bool bCreateDirectory = false, bool bOverwrite = false)
    {
        if (bCreateDirectory && !InDestination.Directory.bExists)
        {
            InDestination.Directory.Create();
        }

        File.Copy(PlatformPath, InDestination.PlatformPath, bOverwrite);
    }

    public string ReadAllText()
    {
        lock (this)
        {
            return File.ReadAllText(PlatformPath);
        }
    }

    public void WriteAllText(string InContents)
    {
        lock (this)
        {
            File.WriteAllText(PlatformPath, InContents);

            UpdateExistance();
        }
    }

    public FileReference ChangeExtension(string NewExtension)
    {
        return Get(Path.ChangeExtension(PlatformPath, NewExtension));
    }

    public override void Delete()
    {
        lock (this)
        {
            File.Delete(PlatformPath);

            UpdateExistance();
        }
    }

    public FileInfo GetInfo()
    {
        return new(PlatformPath);
    }

    protected FileReference(string InFile) : base(InFile)
    {
        Directory = Path.GetDirectoryName(PlatformPath)!;
        Extension = Path.GetExtension(PlatformPath);
        NameWithoutExtension = Path.GetFileNameWithoutExtension(FullPath);

        UpdateExistance();
    }

    public override void UpdateExistance()
    {
        bExists = File.Exists(PlatformPath);
    }

    public static implicit operator FileReference(string InPath)
    {
        return Get(InPath);
    }

    public static implicit operator FileReference(string[] InPathComponents)
    {
        return Get(Path.Combine(InPathComponents));
    }

    protected override string? GetName(string InPath)
    {
        return System.IO.Path.GetFileName(InPath);
    }
}