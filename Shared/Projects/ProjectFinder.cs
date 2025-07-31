using System.Reflection;

namespace Shared.Projects;

using IO;
using Misc;
using Processes;
using Exceptions;
using VisualStudio.CharpProjects;

public static class ProjectFinder 
{
    class CSharpProjectCompilationException(string InMessage) : ABaseException(InMessage);
    class FailedToCreateProjectInstanceException(string InMessage) : ABaseException(InMessage);

    private static readonly Dictionary<string, AProjectDefinition> _loadedProjectsByName = [];
    private static readonly Dictionary<Type, AProjectDefinition> _loadedProjectsByType = [];
    private static readonly Dictionary<string, FileReference> _projectNameSourceMap = [];

    public static void CreateAndCompileProject(DirectoryReference InProjectRootDirectory, string InProjectName)
    {
        DirectoryReference IntermediateBinariesDirectory = InProjectRootDirectory.Combine("Intermediate", "CSharpBinaries");
        DirectoryReference IntermediateProjectsDirectory = InProjectRootDirectory.Combine("Intermediate", "Projects");

        if (!IntermediateBinariesDirectory.bExists) IntermediateBinariesDirectory.Create();
        if (!IntermediateProjectsDirectory.bExists) IntermediateProjectsDirectory.Create();

        FileReference[] ProjectsSources = InProjectRootDirectory.EnumerateFiles("*.Project.cs", SearchOption.AllDirectories);

        FileReference[] AllCSharpSources = [
            .. ProjectsSources,
            .. InProjectRootDirectory.EnumerateFiles("*.Module.cs", SearchOption.AllDirectories),
        ];

        foreach (FileReference ProjectSource in ProjectsSources)
        {
            _projectNameSourceMap.TryAdd(ProjectSource.NameWithoutExtension.Replace(".", ""), ProjectSource);
        }

        FileReference InCSharpProjectFile = IntermediateProjectsDirectory.CombineFile($"Projects.csproj");
        IndentedStringBuilder StringBuilder = new();
        CSharpProject CSharpProject = new(InProjectRootDirectory, AllCSharpSources);
        CSharpProject.Build(StringBuilder);
        InCSharpProjectFile.WriteAllText(StringBuilder.ToString());

        ProcessResult ProcessResult = ProcessExecutorExtension.Run([
            "dotnet",
            "build", $"{InCSharpProjectFile.PlatformPath}",
            "-c", "Debug",
            "-o", $"{IntermediateBinariesDirectory.PlatformPath}",
        ], true);

        if (!ProcessResult.bSuccess) throw new CSharpProjectCompilationException(ProcessResult.StandardOutput);

        // the state of the directory was changed externally so we need to update it manually.
        IntermediateBinariesDirectory.UpdateExistance();
        IntermediateProjectsDirectory.UpdateExistance();

        FileReference[] DllFiles = IntermediateBinariesDirectory.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly);

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            AssemblyName AssemblyName = new(args.Name);
            FileReference DllFile = IntermediateBinariesDirectory.CombineFile($"{AssemblyName.Name!}.dll");
            return DllFile.bExists ? Assembly.LoadFile(DllFile.PlatformPath) : null;
        };

        foreach (FileReference DllFile in DllFiles)
        {
            Assembly ProjectAssembly = Assembly.LoadFile(DllFile.PlatformPath);

            Type[] Types = ProjectAssembly.GetTypes();
            Type[] ProjectTypes = [.. Types.Where(Type => Type.IsClass && !Type.IsAbstract && Type.IsSubclassOf(typeof(AProjectDefinition)))];

            if (ProjectTypes.Length == 0) continue;

            foreach (Type ProjectType in ProjectTypes)
            {
                if (Activator.CreateInstance(ProjectType) is not AProjectDefinition Project)
                {
                    throw new ProjectNotFoundException($"Could not create instance of project: {ProjectType.Name}");
                }

                if (!_loadedProjectsByType.TryAdd(ProjectType, Project) || !_loadedProjectsByName.TryAdd(Project.Name, Project))
                {
                    throw new FailedToCreateProjectInstanceException($"Project type already created: {ProjectType.Name}");
                }
            }
        }
    }

    public static bool HasProject<TProject>()
    {
        return _loadedProjectsByType.ContainsKey(typeof(TProject));
    }

    public static bool HasProject(string InProjectName)
    {
        return _loadedProjectsByName.ContainsKey(InProjectName);
    }

    public static TProject FindProject<TProject>()
        where TProject : AProjectDefinition
    {
        Type ProjectType = typeof(TProject);

        if (!_loadedProjectsByType.TryGetValue(ProjectType, out AProjectDefinition? Project))
        {
            throw new ProjectNotFoundException($"Missing project {ProjectType.Name}");
        }

        Project.Configure(_projectNameSourceMap[ProjectType.Name].Directory);

        return (TProject)Project;
    }

    public static AProjectDefinition FindProject(string InProjectName)
    {
        if (!_loadedProjectsByName.TryGetValue(InProjectName, out AProjectDefinition? Project))
        {
            throw new ProjectNotFoundException($"Missing project {InProjectName}");
        }

        Project.Configure(_projectNameSourceMap[Project.GetType().Name].Directory);

        return Project;
    }
}

public class ProjectNotFoundException(string InMessage) : ABaseException(InMessage);