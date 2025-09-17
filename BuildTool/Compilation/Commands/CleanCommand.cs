using Shared.CommandLines;
using Shared.Commands;
using Shared.Compilation;
using Shared.Extensions;
using Shared.IO;
using Shared.Platforms;
using Shared.Projects;

namespace BuildTool.Compilation.Commands;

public class Clean : IExecutableCommand
{
    public string Name => "Clean";
    public string Example { get; } = string.Join(" ",
        "-Project=/path/to/project",
        "[-Modules=module1,module2,...]",
        $"-Platform=[{string.Join("|", Enum.GetNames<ETargetPlatform>())}]",
        $"-Configuration=[{string.Join('|', Enum.GetNames<ECompileConfiguration>())}]"
    );
    
    public bool Execute(IReadOnlyDictionary<string, ICommandLineArgument> Arguments)
    {
        string ProjectName = Arguments.GetArgumentValue<string>("Project", true) ?? "";
        string PlatformString = Arguments.GetArgumentValue<string>("Platform", true) ?? "";
        string ConfigurationString = Arguments.GetArgumentValue<string>("Configuration", true) ?? "";

        string[] Modules = Arguments.GetArrayArgument<string>("Modules");
        
        ETargetPlatform CompilePlatform = PlatformString.ToEnum<ETargetPlatform>();
        ECompileConfiguration CompileConfiguration = ConfigurationString.ToEnum<ECompileConfiguration>();

        if (!ProjectFinder.HasProject(ProjectName))
        {
            DirectoryReference RootDirectory = Environment.CurrentDirectory;
            ProjectFinder.CreateAndCompileProject(RootDirectory, ProjectName);
        }

        AHostPlatform HostPlatform = AHostPlatform.GetHost();
        if (!HostPlatform.SupportedTargetPlatforms.TryGetValue(CompilePlatform, out ATargetPlatform? TargetPlatform)) throw new TargetPlatformNotSupportedException(HostPlatform, CompilePlatform);

        ATargetPlatform.TargetPlatform = TargetPlatform;

        AProjectDefinition Project = ProjectFinder.FindProject(ProjectName);
        ProjectDirectories.Create(Project, TargetPlatform, CompileConfiguration);

        Dictionary<string, AModuleDefinition> AllModulesMap = [];
        AllModulesMap.AddFrom(Project.GetModules(ETargetPlatform.Any), Project.GetModules(TargetPlatform.Platform));

        AModuleDefinition[] SelectedModules;
        if (Modules is null || Modules.Length == 0)
        {
            SelectedModules = [.. AllModulesMap.Values];
            Console.WriteLine($"WARNING: No module specified, will clean all: {string.Join(", ", AllModulesMap.Keys)}");
        }
        else if (Modules.Length == 1)
        {
            string Module = Modules[0];
            SelectedModules = [AllModulesMap[Module]];
            Console.WriteLine($"Cleaning specified module: {Module}");
        }
        else
        {
            SelectedModules = [.. AllModulesMap.Values.Where(Each => Modules.Contains(Each.Name))];
            Console.WriteLine($"Cleaning specified modules: {string.Join(", ", Modules)}");
        }
        
        Console.WriteLine($"Cleaning Intermediate for platform {TargetPlatform.Name}");

        ProjectDirectories.Shared.CreateIntermediateChecksumsDirectory(TargetPlatform.Platform, CompileConfiguration).Delete(true);

        CleanTask CleanTask = new(Project, SelectedModules, TargetPlatform.Toolchain);
        CleanTask.Clean();

        return true;
    }
}