namespace Shared.Sources;

using IO;
using Projects;
using Platforms;
using Extensions;

public interface ISourceCollection
{
    public string[] HeaderFilesExtensions { get; }
    public string[] SourceFilesExtensions { get; }
    public string[] AllFilesExtensions { get; }

    public FileReference[] HeaderFiles { get; }
    public FileReference[] SourceFiles { get; }
    public FileReference[] AllFiles { get; }

    public void GatherSourceFiles(DirectoryReference InSourceRootDirectory);

    public static ISourceCollection CreateSourceCollection(ETargetPlatform InTargetPlatform, EModuleBinaryType InBinaryType)
    {
        return InTargetPlatform.GetPlatformGroup() switch
        {
            ETargetPlatformGroup.Apple => InBinaryType switch
            {
                EModuleBinaryType.ShaderLibrary => new MetalShaderSourceCollection(),
                _ => new AppleSourceCollection(InTargetPlatform),
            },
            ETargetPlatformGroup.Google => new CppSourceCollection(InTargetPlatform),
            ETargetPlatformGroup.Microsoft => new CppSourceCollection(InTargetPlatform),
            _ => throw new PlatformNotSupportedException(InTargetPlatform),
        };
    }
}