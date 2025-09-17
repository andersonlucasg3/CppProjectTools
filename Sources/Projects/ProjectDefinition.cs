using ProjectTools.Platforms;

namespace ProjectTools.Projects;

using IO;
using Exceptions;
using Extensions;

public abstract class AProjectDefinition : ADefinition
{
    private bool _bIsConfigured = false;

    private DirectoryReference? _rootDirectory = null;
    private DirectoryReference? _modulesDirectory = null;

    private readonly Dictionary<ETargetPlatform, HashSet<AProjectDefinition>> _dependencyProjectsPerPlatform = [];
    private readonly Dictionary<ETargetPlatform, Dictionary<string, AModuleDefinition>> _modulesPerPlatform = [];

    protected virtual string ModulesDirectoryName { get; } = "Modules";

    public DirectoryReference RootDirectory => _rootDirectory!;
    public DirectoryReference ModulesDirectory => _modulesDirectory!;

    public IReadOnlyDictionary<string, AModuleDefinition> GetModules(ETargetPlatform InTargetPlatform)
    {
        if (!_modulesPerPlatform.TryGetValue(InTargetPlatform, out Dictionary<string, AModuleDefinition>? ModuleMap))
        {
            return new Dictionary<string, AModuleDefinition>();
        }

        return ModuleMap;
    }

    public IReadOnlySet<AProjectDefinition> GetDependencyProjects(ETargetPlatform InTargetPlatform)
    {
        if (!_dependencyProjectsPerPlatform.TryGetValue(InTargetPlatform, out HashSet<AProjectDefinition>? ProjectSet))
        {
            return new HashSet<AProjectDefinition>();
        }

        return ProjectSet;
    }

    protected abstract void Configure(ATargetPlatform InTargetPlatform);

    protected void AddProjectDependency<TProject>(ETargetPlatform InTargetPlatform = ETargetPlatform.Any)
        where TProject : AProjectDefinition
    {
        TProject Project = ProjectFinder.FindProject<TProject>();

        if (!_dependencyProjectsPerPlatform.TryGetValue(InTargetPlatform, out HashSet<AProjectDefinition>? ProjectSet))
        {
            ProjectSet = [];
            _dependencyProjectsPerPlatform.Add(InTargetPlatform, ProjectSet);
        }

        ProjectSet.Add(Project);

        foreach (KeyValuePair<ETargetPlatform, Dictionary<string, AModuleDefinition>> Pair in Project._modulesPerPlatform)
        {
            foreach (KeyValuePair<string, AModuleDefinition> ModulePair in Pair.Value)
            {
                AddModuleInternal(Pair.Key, ModulePair.Value);
            }
        }
    }

    protected void AddProjectDependencyToGroup<TProject>(ETargetPlatformGroup InTargetPlatformGroup = ETargetPlatformGroup.Any)
        where TProject : AProjectDefinition
    {
        ETargetPlatform[] Platforms = InTargetPlatformGroup.GetTargetPlatformsInGroup();

        foreach (ETargetPlatform Platform in Platforms)
        {
            AddProjectDependency<TProject>(Platform);
        }
    }

    protected void AddModule<TModule>(ETargetPlatform InTargetPlatform = ETargetPlatform.Any)
        where TModule : AModuleDefinition, new()
    {
        TModule Module = new();

        Module.SetOwnerProject(this);

        AddModuleInternal(InTargetPlatform, Module);
    }

    protected void AddModuleToGroup<TModule>(ETargetPlatformGroup InTargetPlatformGroup = ETargetPlatformGroup.Any)
        where TModule : AModuleDefinition, new()
    {
        ETargetPlatform[] Platforms = InTargetPlatformGroup.GetTargetPlatformsInGroup();

        foreach (ETargetPlatform Platform in Platforms)
        {
            AddModule<TModule>(Platform);
        }
    }

    private void AddModuleInternal(ETargetPlatform InTargetPlatform, AModuleDefinition InModule)
    {
        if (!_modulesPerPlatform.TryGetValue(InTargetPlatform, out Dictionary<string, AModuleDefinition>? ModuleMap))
        {
            ModuleMap = [];
            _modulesPerPlatform.Add(InTargetPlatform, ModuleMap);
        }

        if (ModuleMap.ContainsKey(InModule.Name)) return;

        ModuleMap.Add(InModule.Name, InModule);

        IReadOnlySet<AModuleDefinition> Dependencies = InModule.GetDependencies(InTargetPlatform);
        foreach (AModuleDefinition Dependency in Dependencies)
        {
            AddModuleInternal(InTargetPlatform, Dependency);
        }
    }

    internal void Configure(DirectoryReference InRootDirectory)
    {
        if (_bIsConfigured) return;

        _bIsConfigured = true;

        _rootDirectory = InRootDirectory;
        _modulesDirectory = InRootDirectory.Combine(ModulesDirectoryName);

        Configure(ATargetPlatform.TargetPlatform!);

        foreach (Dictionary<string, AModuleDefinition> Dict in _modulesPerPlatform.Values)
        {
            foreach (AModuleDefinition Module in Dict.Values)
            {
                if (Module.OwnerProject != this) continue;

                Module.Configure(ModulesDirectory.Combine(Module.Name));
            }
        }
    }
}

public class ModuleAlreadyDefinedException(string InModuleName) 
    : ABaseException($"Module '{InModuleName}' already exists in the project definition.");