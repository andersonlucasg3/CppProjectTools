using BuildTool.Compilation.Commands;
using Shared.CommandLines;
using Shared.Commands;
using Shared.Compilation;
using Shared.Extensions;
using Shared.IO;
using Shared.Platforms;
using Shared.Projects;

namespace BuildTool.ProjectGeneration.Commands;

public class GenCodeProject : IExecutableCommand
{
    public string Name => "GenCodeProject";
    public string Example { get; } = string.Join(" ",
        "-Project=/path/to/project",
        $"-Generator=[{string.Join("|", Enum.GetNames<EProjectGeneratorType>())}]", 
        $"-Platform=[{string.Join("|", Enum.GetNames<ETargetPlatform>())}]", 
        $"-Configuration=[{string.Join('|', Enum.GetNames<ECompileConfiguration>())}]"
    );
    
    public bool Execute(IReadOnlyDictionary<string, ICommandLineArgument> Arguments)
    {
        string ProjectName = Arguments.GetArgumentValue<string>("Project", true) ?? "";
        string Generator = Arguments.GetArgumentValue<string>("Generator", true) ?? "";
        string PlatformString = Arguments.GetArgumentValue<string>("Platform", true) ?? "";
        string ConfigurationString = Arguments.GetArgumentValue<string>("Configuration", true) ?? "";

        DirectoryReference RootDirectory = Environment.CurrentDirectory;
        ProjectFinder.CreateAndCompileProject(RootDirectory, ProjectName);

        AProjectDefinition Project = ProjectFinder.FindProject(ProjectName);

        EProjectGeneratorType GeneratorType = Generator.ToEnum<EProjectGeneratorType>();
        ETargetPlatform CompilePlatform = PlatformString.ToEnum<ETargetPlatform>();
        ECompileConfiguration CompileConfiguration = ConfigurationString.ToEnum<ECompileConfiguration>();
        
        AHostPlatform HostPlatform = AHostPlatform.GetHost();
        if (!HostPlatform.SupportedTargetPlatforms.TryGetValue(CompilePlatform, out ATargetPlatform? TargetPlatform)) throw new TargetPlatformNotSupportedException(HostPlatform, CompilePlatform);

        ProjectDirectories.Create(Project, TargetPlatform, CompileConfiguration);
        
        AModuleDefinition[] SelectedModules = [
            .. Project.GetModules(ETargetPlatform.Any).Values,
            .. Project.GetModules(TargetPlatform.Platform).Values
        ];
        
        Dictionary<EProjectGeneratorType, IProjectGenerator> ProjectGenerators = new()
        {
            { EProjectGeneratorType.Clang, new ClangProjectGenerator(SelectedModules, TargetPlatform, CompileConfiguration) },
            { EProjectGeneratorType.VisualStudio, new VisualStudioProjectGenerator(Project, TargetPlatform) },
        };
        
        if (!ProjectGenerators.TryGetValue(GeneratorType, out IProjectGenerator? ProjectGenerator)) if (ProjectGenerator is null) throw new ProjectGeneratorNotSupportedException(Generator);

        ProjectGenerator.Generate();

        return true;
    }
}