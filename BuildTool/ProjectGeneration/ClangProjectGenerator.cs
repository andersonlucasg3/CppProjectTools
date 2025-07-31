using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Compilation;
using Shared.IO;
using Shared.Platforms;
using Shared.Processes;
using Shared.Projects;
using Shared.Toolchains;
using Shared.Toolchains.Compilers;
using Shared.Sources;

namespace BuildTool.ProjectGeneration;

public class ClangCompileCommand
{
    [JsonPropertyName("directory")] public string Directory { get; set; } = "";
    [JsonPropertyName("command")] public string Command { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("output")] public string Output { get; set; } = "";
}

public class ClangProjectGenerator(AModuleDefinition[] InModules, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration)
    : IProjectGenerator
{
    private readonly ProjectDirectories _compileDirectories = ProjectDirectories.Shared;
    
    public void Generate()
    {
        FileReference CompileCommandsJsonFile = "compile_commands.json";

        if (CompileCommandsJsonFile.bExists) CompileCommandsJsonFile.Delete();
        
        List<ClangCompileCommand> CompileCommands = [];
        
        Parallelization.ForEach(InModules, Module =>
        {
            ISourceCollection SourceCollection = ISourceCollection.CreateSourceCollection(InTargetPlatform.Platform, Module.BinaryType);

            SourceCollection.GatherSourceFiles(Module.SourcesDirectory);

            Console.WriteLine($"Processing {SourceCollection.SourceFiles.Length} sources for module: {Module.Name}");

            DirectoryReference ObjectsDirectory = _compileDirectories.CreateIntermediateObjectsDirectory(Module.Name);
            
            Parallelization.ForEach(SourceCollection.SourceFiles, SourceFile =>
            {
                CompileAction Action = new(Module, InTargetPlatform, InConfiguration, SourceFile, ObjectsDirectory, SourceCollection);

                DirectoryReference[] HeaderSearchPaths = [
                    .. Module.GetHeaderSearchPaths(ETargetPlatform.Any),
                    .. Module.GetHeaderSearchPaths(InTargetPlatform.Platform),
                    .. Module.GetDependencies(ETargetPlatform.Any).Select(DependencyModule => DependencyModule.SourcesDirectory),
                    .. Module.GetDependencies(InTargetPlatform.Platform).Select(DependencyModule => DependencyModule.SourcesDirectory)
                ];

                string[] CompilerDefinitions = CompilerDefinitionsProvider.GetAutomaticCompilerDefinitions(InTargetPlatform, InConfiguration, Module);
                
                CompileCommandInfo CompileCommandInfo = new()
                {
                    Module = Module,
                    SourcesDirectory = Module.SourcesDirectory,
                    TargetFile = Action.SourceFile,
                    DependencyFile = Action.DependencyFile,
                    ObjectFile = Action.ObjectFile,
                    HeaderSearchPaths = HeaderSearchPaths,
                    Configuration = InConfiguration,
                    TargetPlatform = InTargetPlatform.Platform,
                    CompilerDefinitions = CompilerDefinitions
                };
                
                string[] CompileCommandline = [.. InTargetPlatform.Toolchain.GetCompileCommandline(CompileCommandInfo)];

                ClangCompileCommand CompileCommand = new()
                {
                    Command = string.Join(" ", CompileCommandline),
                    Directory = Module.SourcesDirectory.PlatformPath,
                    File = SourceFile.PlatformPath,
                    Output = Action.ObjectFile.PlatformPath,
                };
                
                lock (this)
                {
                    CompileCommands.Add(CompileCommand);
                }
            });
        });
        
        Console.WriteLine($"Writing compile_commands.json file to {CompileCommandsJsonFile.Directory}");
        
        CompileCommandsJsonFile.OpenWrite(FileStream =>
        {
            JsonSerializer.Serialize(FileStream, CompileCommands, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        });
    }
}