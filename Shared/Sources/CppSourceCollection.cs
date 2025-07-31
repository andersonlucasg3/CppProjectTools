namespace Shared.Sources;

using IO;
using Processes;
using Platforms;
using Shared.Extensions;

public class CppSourceCollection : ISourceCollection
{
    public static readonly string[] CSourceFilesExtensions = [".c", ".i"];
    public static readonly string[] CppSourceFileExtensions = [".cpp", ".cc", ".cxx", ".c++", ".ii"];
    public static readonly string[] CHeaderFilesExtensions = [".h"];
    public static readonly string[] CppHeaderFilesExtensions = [".hh", ".hpp", ".hxx"];

    public string[] HeaderFilesExtensions { get; private set; } = [];
    public string[] SourceFilesExtensions { get; private set; } = [];
    public string[] AllFilesExtensions { get; private set; } = [];


    public FileReference[] HeaderFiles { get; private set; } = [];
    public FileReference[] SourceFiles { get; private set; } = [];
    public FileReference[] AllFiles { get; private set; } = [];

    private string[] ExcludedPlatforms;
    private string[] ExcludedPlatformGroups;
    private string[] ExcludedPlatformTypes;

    public CppSourceCollection(ETargetPlatform InTargetPlatform)
    {
        List<ETargetPlatform> ExcludedPlatformsList = [.. Enum.GetValues<ETargetPlatform>()];
        ExcludedPlatformsList.Remove(ETargetPlatform.Any);
        ExcludedPlatformsList.Remove(InTargetPlatform); // remove the current platform so we won't exclude it

        List<ETargetPlatformGroup> ExcludedPlatformGroupsList = [.. Enum.GetValues<ETargetPlatformGroup>()];
        ExcludedPlatformGroupsList.Remove(ETargetPlatformGroup.Any);
        ExcludedPlatformGroupsList.Remove(InTargetPlatform.GetPlatformGroup());

        List<ETargetPlatformType> ExcludedPlatformTypesList = [.. Enum.GetValues<ETargetPlatformType>()];
        ExcludedPlatformTypesList.Remove(ETargetPlatformType.Any);
        ExcludedPlatformTypesList.Remove(InTargetPlatform.GetPlatformType());

        ExcludedPlatforms = [.. ExcludedPlatformsList.Select(Each => $"/{Each.ToSourcePlatformName()}/")];
        ExcludedPlatformGroups = [.. ExcludedPlatformGroupsList.Select(Each => $"/{Each}/")];
        ExcludedPlatformTypes = [.. ExcludedPlatformTypesList.Select(Each => $"/{Each}/")];
    }

    public void GatherSourceFiles(DirectoryReference InSourcesRootDirectory)
    {
        HeaderFilesExtensions = GetHeaderFilesExtensions();
        SourceFilesExtensions = GetSourceFilesExtensions();
        AllFilesExtensions = [.. HeaderFilesExtensions, .. SourceFilesExtensions];

        List<FileReference> HeadersList = [];
        List<FileReference> SourcesList = [];

        Action[] Actions = [
            () => {
                Parallelization.ForEach(HeaderFilesExtensions, HeaderFileExtension =>
                {
                    FileReference[] Headers = [.. InSourcesRootDirectory.EnumerateFiles($"*{HeaderFileExtension}", SearchOption.AllDirectories)];

                    lock (this)
                    {
                        foreach (FileReference Header in Headers)
                        {
                            if (ExcludeSource(Header)) continue;

                            HeadersList.Add(Header);
                        }
                    }
                });
            },
            () =>
            {
                Parallelization.ForEach(SourceFilesExtensions, SourceFileExtension =>
                {
                    FileReference[] Sources = [.. InSourcesRootDirectory.EnumerateFiles($"*{SourceFileExtension}", SearchOption.AllDirectories)];

                    lock (this)
                    {
                        foreach (FileReference Source in Sources)
                        {
                            if(ExcludeSource(Source)) continue;

                            SourcesList.Add(Source);
                        }
                    }
                });
            }
        ];

        Parallelization.ForEach(Actions, Action => Action.Invoke());

        HeaderFiles = [.. HeadersList];
        SourceFiles = [.. SourcesList];

        AllFiles = [.. HeaderFiles, .. SourceFiles];
    }

    protected virtual string[] GetHeaderFilesExtensions()
    {
        return [
            .. CHeaderFilesExtensions,
            .. CppHeaderFilesExtensions,
        ];
    }

    protected virtual string[] GetSourceFilesExtensions()
    {
        return [
            .. CSourceFilesExtensions,
            .. CppSourceFileExtensions,
        ];
    }

    private bool ExcludeSource(FileReference InSource)
    {
        return ExcludedPlatforms.Any(InSource.RelativePath.Contains) ||
               ExcludedPlatformGroups.Any(InSource.RelativePath.Contains) ||
               ExcludedPlatformTypes.Any(InSource.RelativePath.Contains);
    }
}