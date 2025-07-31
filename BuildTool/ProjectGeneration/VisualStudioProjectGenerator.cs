using Shared.IO;
using Shared.Misc;
using Shared.Sources;
using Shared.Projects;
using Shared.Processes;
using Shared.Platforms;
using Shared.Extensions;
using Shared.Compilation;
using Shared.Projects.VisualStudio.Filters;
using Shared.Projects.VisualStudio.Projecs;
using Shared.Projects.VisualStudio.Solutions;

namespace BuildTool.ProjectGeneration;

public class VisualStudioProjectGenerator(AProjectDefinition InProjectDefinition, ATargetPlatform InTargetPlatform) : IProjectGenerator
{
    private readonly ECompileConfiguration[] _compileConfigurations = Enum.GetValues<ECompileConfiguration>();

    public void Generate()
    {
        DirectoryReference ProjectsDirectory = ProjectDirectories.Shared.CreateIntermediateProjectsDirectory();

        // TODO: check on multi-module projects if this will cause any pain
        // string ProgramsDirectory = Path.Combine(Environment.CurrentDirectory, "Programs");
        FileReference[] CSharpProjects = [.. Directory.EnumerateFiles(Environment.CurrentDirectory, "*.csproj", SearchOption.AllDirectories)];

        Dictionary<string, AModuleDefinition> Modules = [];
        Modules.AddFrom(InProjectDefinition.GetModules(ETargetPlatform.Any), InProjectDefinition.GetModules(InTargetPlatform.Platform));

        FileReference SolutionFile = $"{InProjectDefinition.Name}.sln";
        Solution Solution = GenerateSolutionFile(SolutionFile, ProjectsDirectory, CSharpProjects, Modules, out Dictionary<AModuleDefinition, FileReference>? ModuleVcxProjFileMap);

        if (ModuleVcxProjFileMap is not null)
        {
            Parallelization.ForEach([.. Solution.Projects], Project =>
            {
                if (!Modules.TryGetValue(Project.ProjectName, out AModuleDefinition? Module)) return;

                AModuleDefinition[] ModuleDependencies = [
                    .. Module.GetDependencies(ETargetPlatform.Any),
                    .. Module.GetDependencies(InTargetPlatform.Platform)
                ];

                SolutionProject[] Dependencies = [.. ModuleDependencies.Select(DependencyModule => Solution.Projects.First(Project => Project.ProjectName == DependencyModule.Name))];

                GenerateVCXProj(InProjectDefinition.Name, Module, ModuleDependencies, Project, Dependencies, ModuleVcxProjFileMap[Module]);
            });
        }
    }

    private Solution GenerateSolutionFile(FileReference InSolutionFile, DirectoryReference InProjectsDirectory, FileReference[] InCSharpProjectFiles, Dictionary<string, AModuleDefinition> InModules, out Dictionary<AModuleDefinition, FileReference>? OutModuleProjectFileMap)
    {
        OutModuleProjectFileMap = null;

        IndentedStringBuilder StringBuilder = new();

        Dictionary<SolutionProject, SolutionProject> NestedProjectsMap = [];

        SolutionProject ProgramsFolder = new("Programs", "Programs", [], [], ESolutionProjectKind.Folder);
        SolutionProject[] CSharpProjects = [.. InCSharpProjectFiles.Select(File => new SolutionProject(File.NameWithoutExtension, File.RelativePath, _compileConfigurations, [ETargetPlatform.Any], ESolutionProjectKind.CSharpProject))];
        Array.ForEach(CSharpProjects, Project => NestedProjectsMap.Add(Project, ProgramsFolder));

        List<SolutionProject> Projects = [
            ProgramsFolder,
            .. CSharpProjects,
        ];

        if (AHostPlatform.IsWindows())
        {
            OutModuleProjectFileMap = InModules.Values.ToDictionary(Module => Module, Module => InProjectsDirectory.CombineFile($"{Module.Name}.vcxproj"));

            SolutionProject ModulesFolder = new("Modules", "Modules", [], [], ESolutionProjectKind.Folder);
            SolutionProject[] ModulesProjects = [.. OutModuleProjectFileMap.Select(KVPair => new SolutionProject(KVPair.Key.Name, KVPair.Value.RelativePath, _compileConfigurations, [InTargetPlatform.Platform]))];
            Array.ForEach(ModulesProjects, Project => NestedProjectsMap.Add(Project, ModulesFolder));

            Projects.AddRange([
                ModulesFolder,
                .. ModulesProjects,
            ]);
        }

        Solution Solution = new([.. Projects], NestedProjectsMap);

        Solution.Build(StringBuilder);
        
        InSolutionFile.WriteAllText(StringBuilder.ToString());

        return Solution;
    }

    private void GenerateVCXProj(string InProjectName, AModuleDefinition InModule, AModuleDefinition[] InModuleDependencies, SolutionProject InProject, SolutionProject[] Dependencies, FileReference InVcxProjFile)
    {
        if (InProject.ProjectKind != ESolutionProjectKind.CppProject) return;

        IndentedStringBuilder StringBuilder = new();

        DirectoryReference[] DependenciesSourcesDirectories = [.. InModuleDependencies.Select(Dependency => Dependency.SourcesDirectory)];

        ISourceCollection SourceCollection = ISourceCollection.CreateSourceCollection(InTargetPlatform.Platform, InModule.BinaryType);
        SourceCollection.GatherSourceFiles(InModule.SourcesDirectory);

        Project Project = new(new ProjectDependencies
        {
            ProjectName = InProjectName,
            Project = InProject,
            Dependencies = Dependencies,
            BinaryType = InModule.BinaryType,
            IntermediateDirectory = ProjectDirectories.CreateBaseDirectory(ECompileBaseDirectory.Intermediate),
            BinariesDirectory = ProjectDirectories.CreateBaseDirectory(ECompileBaseDirectory.Binaries),
            SourcesCollection = SourceCollection,
            ProjectSourcesDirectory = InModule.SourcesDirectory,
            DependenciesSourcesDirectories = DependenciesSourcesDirectories,
            PreprocessorDefinitions = [
                .. InTargetPlatform.Toolchain.GetAutomaticModuleCompilerDefinitions(InModule, InTargetPlatform.Platform),
                .. InModule.GetCompilerDefinitions(ETargetPlatform.Any),
                .. InModule.GetCompilerDefinitions(InTargetPlatform.Platform)
            ]
        });

        Project.Build(StringBuilder);

        InVcxProjFile.WriteAllText(StringBuilder.ToString());

        // now the project filters file

        ProjectFilters ProjectFilters = new(InModule.RootDirectory, SourceCollection);

        StringBuilder.Clear();

        ProjectFilters.Build(StringBuilder);

        FileReference FiltersFile = InVcxProjFile.ChangeExtension(".vcxproj.filters");

        FiltersFile.WriteAllText(StringBuilder.ToString());
    }
}
