namespace ProjectTools.Sources;

using IO;


public class MetalShaderSourceCollection : ISourceCollection
{
    public static string[] MetalHeaderFilesExtensions = [".h", ".hpp"];
    public static string[] MetalSourceFilesExtensions = [".metal"];

    public string[] HeaderFilesExtensions => MetalHeaderFilesExtensions;
    public string[] SourceFilesExtensions => MetalSourceFilesExtensions;
    public string[] AllFilesExtensions { get; } = [.. MetalHeaderFilesExtensions, .. MetalSourceFilesExtensions];

    public FileReference[] HeaderFiles { get; } = [];

    public FileReference[] SourceFiles { get; private set; } = [];

    public FileReference[] AllFiles { get; private set; } = [];

    public void GatherSourceFiles(DirectoryReference InSourceRootDirectory)
    {
        AllFiles = SourceFiles = InSourceRootDirectory.EnumerateFiles("*.metal", SearchOption.AllDirectories);
    }
}