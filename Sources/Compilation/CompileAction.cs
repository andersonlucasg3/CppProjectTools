namespace ProjectTools.Compilation;

using IO;
using Projects;
using ProjectTools.Platforms;
using ProjectTools.Toolchains.Compilers;
using Sources;
using Toolchains;

public class CompileAction
{
    private readonly string _objectFileExtension;
    private readonly ISourceCollection _sourceCollection;

    private CompileDependency? _dependency = null;

    public readonly FileReference SourceFile;
    public readonly FileReference DependencyFile;
    public readonly FileReference ObjectFile;
    
    public readonly CompileCommandInfo CompileCommandInfo;

    public CompileDependency Dependency
    {
        get
        {
            DependencyFile.UpdateExistance();
            if (_dependency is null && DependencyFile.bExists)
            {
                _dependency = new(DependencyFile, _objectFileExtension, _sourceCollection);
            }

            return _dependency!;
        }
    }

    public string KeyName => SourceFile.RelativePath;

    public CompileAction(AModuleDefinition InModule, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration, ETargetArch InArch, FileReference InSourceFile, DirectoryReference InObjectsDirectory, ISourceCollection InSourceCollection)
    {
        _sourceCollection = InSourceCollection;

        SourceFile = InSourceFile;

        _objectFileExtension = InTargetPlatform.Toolchain.GetObjectFileExtension(InSourceFile);
        ObjectFile = InObjectsDirectory.CombineFile($"{SourceFile.Name}{_objectFileExtension}");
        DependencyFile = $"{ObjectFile.FullPath}.d";

        // header search paths are PUBLIC: a module exposes its own paths (Any + target platform) to every
        // module that depends on it, so a dependency's external headers do not have to be declared again
        DirectoryReference[] HeaderSearchPathCandidates = [
            .. InModule.GetHeaderSearchPaths(ETargetPlatform.Any),
            .. InModule.GetHeaderSearchPaths(InTargetPlatform.Platform),
            .. InModule.GetDependencies(ETargetPlatform.Any).SelectMany(DependencyModule => DependencyModule.GetHeaderSearchPaths(ETargetPlatform.Any)),
            .. InModule.GetDependencies(ETargetPlatform.Any).SelectMany(DependencyModule => DependencyModule.GetHeaderSearchPaths(InTargetPlatform.Platform)),
            .. InModule.GetDependencies(InTargetPlatform.Platform).SelectMany(DependencyModule => DependencyModule.GetHeaderSearchPaths(ETargetPlatform.Any)),
            .. InModule.GetDependencies(InTargetPlatform.Platform).SelectMany(DependencyModule => DependencyModule.GetHeaderSearchPaths(InTargetPlatform.Platform)),
            .. InModule.GetDependencies(ETargetPlatform.Any).Select(DependencyModule => DependencyModule.SourcesDirectory),
            .. InModule.GetDependencies(InTargetPlatform.Platform).Select(DependencyModule => DependencyModule.SourcesDirectory),
        ];

        // several dependencies can expose the same path, so collapse them before they reach /I
        DirectoryReference[] HeaderSearchPaths = [.. HeaderSearchPathCandidates.DistinctBy(HeaderSearchPath => HeaderSearchPath.PlatformPath)];

        string[] CompilerDefinitions = CompilerDefinitionsProvider.GetAutomaticCompilerDefinitions(InTargetPlatform, InConfiguration, InModule);
        
        CompileCommandInfo = new()
        {
            Module = InModule,
            SourcesDirectory = InModule.SourcesDirectory,
            TargetFile = SourceFile,
            DependencyFile = DependencyFile,
            ObjectFile = ObjectFile,
            HeaderSearchPaths = HeaderSearchPaths,
            Configuration = InConfiguration,
            TargetPlatform = InTargetPlatform.Platform,
            TargetArch = InArch,
            CompilerDefinitions = CompilerDefinitions
        };
    }
}

public class LinkAction
{
    public readonly FileReference LinkedFile;
    
    public LinkAction(AModuleDefinition InModule, IToolchain InToolchain)
    {
        string Prefix = InToolchain.GetBinaryTypePrefix(InModule.BinaryType);
        string Extension = InToolchain.GetBinaryTypeExtension(InModule.BinaryType);
        
        DirectoryReference ConfigurationDirectory = ProjectDirectories.Shared.CreateBaseConfigurationDirectory(ECompileBaseDirectory.Binaries);
        LinkedFile = ConfigurationDirectory.CombineFile($"{Prefix}{InModule.OutputName}{Extension}");
    }
}