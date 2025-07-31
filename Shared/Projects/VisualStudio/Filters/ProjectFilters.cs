using Shared.IO;
using Shared.Misc;
using Shared.Sources;

namespace Shared.Projects.VisualStudio.Filters;

using ProjectXml;

public class ProjectFilters : TTagGroup<IIndentedStringBuildable>
{
    protected override string TagName => "Project";
    
    protected override Parameter[] Parameters => [
        new Parameter("ToolsVersion", "Current"),
        new Parameter("xmlns", XmlHeader.XmlNamespace),
    ];

    protected override IIndentedStringBuildable[] Contents { get; }

    public ProjectFilters(DirectoryReference InModuleSourceDirectory, ISourceCollection InSourceCollection)
    {
        HashSet<string> UniquePaths = [];
        Dictionary<FileReference, string> SourcesFilterMap = [];
        Dictionary<FileReference, string> HeadersFilterMap = [];

        ProcessFiles(InModuleSourceDirectory, InSourceCollection.SourceFiles, UniquePaths, SourcesFilterMap);
        ProcessFiles(InModuleSourceDirectory, InSourceCollection.HeaderFiles, UniquePaths, HeadersFilterMap);

        Contents = [
            new Filters([.. UniquePaths], InSourceCollection),
            new Sources(SourcesFilterMap),
            new Headers(HeadersFilterMap),
        ];
    }

    public override void Build(IndentedStringBuilder InStringBuilder)
    {
        XmlHeader.Build(InStringBuilder);

        base.Build(InStringBuilder);
    }

    private static void ProcessFiles(DirectoryReference InModuleSourceDirectory, FileReference[] InFiles, HashSet<string> InUniquePaths, Dictionary<FileReference, string> InFileFilterMap)
    {
        foreach (FileReference File in InFiles)
        {
            string[] PathComponents = File.Directory.RelativePathComponents(InModuleSourceDirectory);

            string CurrentPath = string.Empty;

            foreach (string PathComponent in PathComponents)
            {
                CurrentPath = string.IsNullOrEmpty(CurrentPath)
                    ? PathComponent
                    : $"{CurrentPath}\\{PathComponent}";

                InUniquePaths.Add(CurrentPath);
            }

            InFileFilterMap[File] = CurrentPath;
        }
    }
}