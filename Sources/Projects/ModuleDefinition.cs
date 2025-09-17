namespace ProjectTools.Projects;

using IO;
using Platforms;
using Exceptions;
using ProjectTools.Platforms;
using ProjectTools.Extensions;

public abstract class AModuleDefinition : ADefinition
{
    private bool _bIsConfigured = false;
    private AProjectDefinition? _ownerProject = null;
    private DirectoryReference? _rootDirectory = null;
    private DirectoryReference? _sourcesDirectory = null;

    private readonly Dictionary<ETargetPlatform, HashSet<string>> _headerSearchPathPerPlatform = [];
    private readonly Dictionary<ETargetPlatform, HashSet<AModuleDefinition>> _dependenciesPerPlatform = [];
    private readonly Dictionary<ETargetPlatform, HashSet<string>> _compilerDefinitionsPerPlatform = [];
    private readonly Dictionary<ETargetPlatform, HashSet<string>> _linkWithLibrariesPerPlatform = [];

    private readonly List<string> _librarySearchPaths = [];

    private readonly List<DirectoryReference> _copyResourcesDirectories = [];

    protected virtual string SourcesDirectoryName { get; } = "Sources";

    // can be overriden to change the output file name
    // for example in a library or application module with name X
    // the output linked file can have name Y
    public virtual string OutputName => Name;

    public abstract EModuleBinaryType BinaryType { get; }

    public AProjectDefinition OwnerProject => _ownerProject!;

    public IReadOnlyList<string> LibrarySearchPaths => _librarySearchPaths;
    public IReadOnlyList<DirectoryReference> CopyResourcesDirectories => _copyResourcesDirectories;

    public PlatformSpecifics PlatformSpecifics { get; } = new();

    public DirectoryReference RootDirectory => _rootDirectory!;
    public DirectoryReference SourcesDirectory => _sourcesDirectory!;

    protected abstract void Configure(ATargetPlatform InTargetPlatform);

    public IReadOnlySet<AModuleDefinition> GetDependencies(ETargetPlatform InTargetPlatform = ETargetPlatform.Any)
    {
        if (!_dependenciesPerPlatform.TryGetValue(InTargetPlatform, out HashSet<AModuleDefinition>? ModuleSet))
        {
            return new HashSet<AModuleDefinition>();
        }

        return ModuleSet;
    }

    public IReadOnlySet<string> GetHeaderSearchPaths(ETargetPlatform InTargetPlatform = ETargetPlatform.Any)
    {
        if (!_headerSearchPathPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? SearchPathsSet))
        {
            return new HashSet<string>();
        }

        return SearchPathsSet;
    }

    public IReadOnlySet<string> GetCompilerDefinitions(ETargetPlatform InTargetPlatform = ETargetPlatform.Any)
    {
        if (!_compilerDefinitionsPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? CompilerDefinitionsSet))
        {
            return new HashSet<string>();
        }

        return CompilerDefinitionsSet;
    }

    public IReadOnlySet<string> GetLinkWithLibraries(ETargetPlatform InTargetPlatform)
    {
        if (!_linkWithLibrariesPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? LinkWithLibrariesSet))
        {
            return new HashSet<string>();
        }

        return LinkWithLibrariesSet;
    }

    protected void AddDependencyModuleNames(params string[] InModuleNames)
    {
        AddDependencyModuleNames(ETargetPlatform.Any, InModuleNames);
    }

    protected void AddDependencyModuleNames(ETargetPlatform InTargetPlatform, params string[] InModuleNames)
    {
        IReadOnlyDictionary<string, AModuleDefinition> ModulesMap = OwnerProject.GetModules(InTargetPlatform);

        if (!_dependenciesPerPlatform.TryGetValue(InTargetPlatform, out HashSet<AModuleDefinition>? ModuleSet))
        {
            ModuleSet = [];
            _dependenciesPerPlatform.Add(InTargetPlatform, ModuleSet);
        }

        foreach (string ModuleName in InModuleNames)
        {
            if (!ModulesMap.TryGetValue(ModuleName, out AModuleDefinition? DependencyModule))
            {
                throw new MissingDependencyModuleException(ModuleName, OwnerProject.Name);
            }

            ModuleSet.Add(DependencyModule);

            AddDependencyRecursively(DependencyModule);
        }
    }

    protected void AddDependencyModuleNames(ETargetPlatformGroup InGroup, params string[] InModuleNames)
    {
        foreach (ETargetPlatform Platform in InGroup.GetTargetPlatformsInGroup())
        {
            AddDependencyModuleNames(Platform, InModuleNames);
        }
    }

    protected void AddHeaderSearchPaths(params string[] InHeaderSearchPaths)
    {
        AddHeaderSearchPaths(ETargetPlatform.Any, InHeaderSearchPaths);
    }

    protected void AddHeaderSearchPaths(ETargetPlatform InTargetPlatform, params string[] InHeaderSearchPaths)
    {
        if (!_headerSearchPathPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? SearchPathsSet))
        {
            SearchPathsSet = [];
            _headerSearchPathPerPlatform.Add(InTargetPlatform, SearchPathsSet);
        }

        foreach (string HeaderSearchPath in InHeaderSearchPaths)
        {
            // TODO: check this
            DirectoryReference SearchPath = OwnerProject.ModulesDirectory.Combine(HeaderSearchPath);
            SearchPathsSet.Add(SearchPath.PlatformRelativePath);
        }
    }

    protected void AddCompilerDefinition(string InDefine, string? InValue = null)
    {
        AddCompilerDefinition(ETargetPlatform.Any, InDefine, InValue);
    }

    protected void AddCompilerDefinition(ETargetPlatform InTargetPlatform, string InDefine, string? InValue = null)
    {
        if (!_compilerDefinitionsPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? CompilerDefinitionSet))
        {
            CompilerDefinitionSet = [];
            _compilerDefinitionsPerPlatform.Add(InTargetPlatform, CompilerDefinitionSet);
        }

        if (string.IsNullOrEmpty(InValue))
        {
            CompilerDefinitionSet.Add(InDefine.ToUpper());

            return;
        }

        CompilerDefinitionSet.Add($"{InDefine.ToUpper()}={InValue}");
    }

    protected void AddLinkWithLibrary(ETargetPlatform InTargetPlatform, params string[] InLibraries)
    {
        if (!_linkWithLibrariesPerPlatform.TryGetValue(InTargetPlatform, out HashSet<string>? LinkWithLibrariesSet))
        {
            LinkWithLibrariesSet = [];
            _linkWithLibrariesPerPlatform.Add(InTargetPlatform, LinkWithLibrariesSet);
        }

        foreach (string Library in InLibraries)
        {
            LinkWithLibrariesSet.Add(Library);
        }
    }

    protected void AddLibrarySearchPaths(params string[] InLibrarySearchPaths)
    {
        foreach (string LibrarySearchPath in InLibrarySearchPaths)
        {
            _librarySearchPaths.Add(LibrarySearchPath);
        }
    }

    protected void AddCopyResourcesFolders(params string[] InCopyResourcesFolders)
    {
        foreach (string CopyResourcesFolder in InCopyResourcesFolders)
        {
            DirectoryReference ResourcesDirectory = SourcesDirectory.Combine(CopyResourcesFolder);

            if (!ResourcesDirectory.bExists)
            {
                Console.WriteLine($"ERROR: not a directory {ResourcesDirectory.PlatformRelativePath}");

                continue;
            }

            _copyResourcesDirectories.Add(ResourcesDirectory);
        }
    }

    internal void SetOwnerProject(AProjectDefinition InOwnerProject)
    {
        _ownerProject = InOwnerProject;
    }

    internal void Configure(DirectoryReference InRootDirectory)
    {
        if (_bIsConfigured) return;

        _bIsConfigured = true;

        _rootDirectory = InRootDirectory;
        _sourcesDirectory = InRootDirectory.Combine(SourcesDirectoryName);

        Configure(ATargetPlatform.TargetPlatform!);
    }

    private void AddDependencyRecursively(AModuleDefinition InDependency)
    {
        foreach (KeyValuePair<ETargetPlatform, HashSet<AModuleDefinition>> Pair in InDependency._dependenciesPerPlatform)
        {
            if (!_dependenciesPerPlatform.TryGetValue(Pair.Key, out HashSet<AModuleDefinition>? ModuleSet))
            {
                ModuleSet = [];
                _dependenciesPerPlatform.Add(Pair.Key, ModuleSet);
            }

            foreach (AModuleDefinition DependencyModule in Pair.Value)
            {
                ModuleSet.Add(DependencyModule);

                AddDependencyRecursively(DependencyModule);
            }
        }
    }
}

public class MissingDependencyModuleException(string InModuleName, string InProjectName) 
    : ABaseException($"Module '{InModuleName}' not found in project '{InProjectName}'.");
public class ShaderLibraryNotSupportedOnPlatformException(EModuleBinaryType InBinaryType) : ABaseException($"{InBinaryType}");
public class UnsupportedDependencyForCodeModuleException(string InModuleName) 
    : ABaseException($"Module '{InModuleName}' not supported as code dependency.");