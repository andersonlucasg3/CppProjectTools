using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Compilation;
using Shared.IO;
using Shared.Platforms;
using Shared.Processes;
using Shared.Projects;
using Shared.Toolchains;

namespace BuildTool.ProjectGeneration;

public class ClangCompileCommand
{
    [JsonPropertyName("directory")] public string Directory { get; set; } = "";
    [JsonPropertyName("command")] public string Command { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("output")] public string Output { get; set; } = "";
}

public class ClangProjectGenerator(ModuleDefinition[] InModules, ITargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration)
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
            Console.WriteLine($"Processing {Module.Sources.SourceFiles.Length} sources for module: {Module.Name}");

            DirectoryReference ObjectsDirectory = _compileDirectories.CreateIntermediateObjectsDirectory(Module.Name);
            
            Parallelization.ForEach(Module.Sources.SourceFiles, SourceFile =>
            {
                CompileAction Action = new(SourceFile, ObjectsDirectory, InTargetPlatform.Toolchain, Module.Sources);

                DirectoryReference[] HeaderSearchPaths = [
                    .. Module.GetHeaderSearchPaths(ETargetPlatform.Any),
                    .. Module.GetHeaderSearchPaths(InTargetPlatform.Platform),
                    .. Module.GetDependencies(ETargetPlatform.Any).Select(DependencyModule => DependencyModule.SourcesDirectory),
                    .. Module.GetDependencies(InTargetPlatform.Platform).Select(DependencyModule => DependencyModule.SourcesDirectory)
                ];
                
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