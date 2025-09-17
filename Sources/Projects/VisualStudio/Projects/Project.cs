using ProjectTools.IO;
using ProjectTools.Misc;
using ProjectTools.Sources;
using ProjectTools.Platforms;
using ProjectTools.Compilation;

namespace ProjectTools.Projects.VisualStudio.Projecs;

using Solutions;
using ProjectXml;

public struct ProjectDependencies
{
    public required string ProjectName;
    public required SolutionProject Project;
    public required SolutionProject[] Dependencies;

    public required EModuleBinaryType BinaryType;

    public required DirectoryReference IntermediateDirectory;
    public required DirectoryReference BinariesDirectory;
    public required ISourceCollection SourcesCollection;

    public required DirectoryReference ProjectSourcesDirectory;
    public required DirectoryReference[] DependenciesSourcesDirectories;

    public required string[] PreprocessorDefinitions;
}

public class Project : TTagGroup<IIndentedStringBuildable>
{
    protected override string TagName => "Project";
    
    protected override Parameter[] Parameters => [
        new Parameter("DefaultTargets", "Build"),
        new Parameter("ToolsVersion", "Current"),
        new Parameter("xmlns", XmlHeader.XmlNamespace),
    ];

    protected override IIndentedStringBuildable[] Contents { get; }

    public Project(ProjectDependencies InProjectDependencies)
    {
        List<IIndentedStringBuildable> ContentsList = [
            new ProjectConfigurations(InProjectDependencies.Project.CompileConfigurations, InProjectDependencies.Project.TargetPlatforms),
            new Globals(InProjectDependencies.Project, InProjectDependencies.BinaryType, InProjectDependencies.IntermediateDirectory, InProjectDependencies.BinariesDirectory),
            new Import("$(VCTargetsPath)\\Microsoft.Cpp.Default.props"),
            new Import("$(VCTargetsPath)\\Microsoft.Cpp.props"),
            new Import("$(VCTargetsPath)\\Microsoft.Cpp.targets"),
            new Target("Build", $"pwsh -c ./Programs/Scripts/Compile.ps1 -Project {InProjectDependencies.ProjectName} -Platform $(Platform) -Configuration $(Configuration) -Modules $(ProjectName)"),
            new Target("Clean", $"pwsh -c ./Programs/Scripts/Compile.ps1 -Project {InProjectDependencies.ProjectName} -Platform $(Platform) -Configuration $(Configuration) -Modules $(ProjectName) -Clean"),
            new Target("Rebuild", InExtraParameters: [new Parameter("DependsOnTargets", "Clean;Build")]),
            new Headers(InProjectDependencies.SourcesCollection),
            new Sources(InProjectDependencies.SourcesCollection),
            new Dependencies(InProjectDependencies.Dependencies),
        ];

        for (int CompileConfigurationIndex = 0; CompileConfigurationIndex < InProjectDependencies.Project.CompileConfigurations.Length; CompileConfigurationIndex++)
        {
            ECompileConfiguration Configuration = InProjectDependencies.Project.CompileConfigurations[CompileConfigurationIndex];

            for (int PlatformIndex = 0; PlatformIndex < InProjectDependencies.Project.TargetPlatforms.Length; PlatformIndex++)
            {
                ETargetPlatform Platform = InProjectDependencies.Project.TargetPlatforms[PlatformIndex];

                ItemDefinitionGroup NewItemGroup = new(
                    InProjectDependencies.PreprocessorDefinitions,
                    [
                        InProjectDependencies.ProjectSourcesDirectory,
                        .. InProjectDependencies.DependenciesSourcesDirectories
                    ],
                    Configuration,
                    Platform);

                ContentsList.Add(NewItemGroup);
            }
        }

        Contents = [.. ContentsList];
    }

    public override void Build(IndentedStringBuilder InStringBuilder)
    {
        XmlHeader.Build(InStringBuilder);

        base.Build(InStringBuilder);
    }
}
