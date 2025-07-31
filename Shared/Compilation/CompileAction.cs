namespace Shared.Compilation;

using IO;
using Projects;
using Shared.Platforms;
using Shared.Toolchains.Compilers;
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

    public CompileAction(AModuleDefinition InModule, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration, FileReference InSourceFile, DirectoryReference InObjectsDirectory, ISourceCollection InSourceCollection)
    {
        _sourceCollection = InSourceCollection;

        SourceFile = InSourceFile;

        _objectFileExtension = InTargetPlatform.Toolchain.GetObjectFileExtension(InSourceFile);
        ObjectFile = InObjectsDirectory.CombineFile($"{SourceFile.Name}{_objectFileExtension}");
        DependencyFile = $"{ObjectFile.FullPath}.d";

        DirectoryReference[] HeaderSearchPaths = [
            .. InModule.GetHeaderSearchPaths(ETargetPlatform.Any),
            .. InModule.GetHeaderSearchPaths(InTargetPlatform.Platform),
            .. InModule.GetDependencies(ETargetPlatform.Any).Select(DependencyModule => DependencyModule.SourcesDirectory),
            .. InModule.GetDependencies(InTargetPlatform.Platform).Select(DependencyModule => DependencyModule.SourcesDirectory),
        ];

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