using Shared.Compilation;
using Shared.IO;
using Shared.Platforms;
using Shared.Processes;
using Shared.Projects;

namespace Shared.Toolchains;

public struct CompileCommandInfo
{
    public required AModuleDefinition Module;
    public required DirectoryReference SourcesDirectory;
    public required FileReference TargetFile;
    public required FileReference DependencyFile;
    public required FileReference ObjectFile;
    public required DirectoryReference[] HeaderSearchPaths;
    public required ECompileConfiguration Configuration;
    public required ETargetPlatform TargetPlatform;
    public required string[] CompilerDefinitions;
}

public struct LinkCommandInfo
{
    public required ETargetPlatform Platform;
    public required AModuleDefinition Module;
    public required FileReference LinkedFile;
    public required FileReference[] ObjectFiles;
    public required DirectoryReference[] LibrarySearchPaths;
    public required ETargetPlatform TargetPlatform;
    public required ECompileConfiguration Configuration;
    public required string[] LinkWithLibraries;
}

public interface IToolchain
{
    public string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo);
    public string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo);
    public string GetBinaryTypeExtension(EModuleBinaryType BinaryType);
    public string GetBinaryTypePrefix(EModuleBinaryType BinaryType);
    public string GetObjectFileExtension(FileReference InSourceFile);

    public string[] GetAutomaticModuleCompilerDefinitions(AModuleDefinition InModule, ETargetPlatform InTargetPlatform);

    public ProcessResult Compile(CompileCommandInfo InCompileCommandInfo);
    public ProcessResult Link(LinkCommandInfo InLinkCommandInfo);
}