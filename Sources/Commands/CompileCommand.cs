using ProjectTools.CommandLines;
using ProjectTools.Compilation;
using ProjectTools.Exceptions;
using ProjectTools.Platforms;
using ProjectTools.Projects;
using ProjectTools.Extensions;
using ProjectTools.Processes;
using ProjectTools.IO;
using ProjectTools.Sources;
using ProjectTools.Tasks;
using ProjectTools.Compilation.Actions;

namespace ProjectTools.Commands;

public class Compile : IExecutableCommand
{
    public string Name => "Compile";
    public string Example { get; } = string.Join(" ",
        "-Project=/path/to/project",
        "[-Modules=module1,module2,...]",
        $"-Platform=[{string.Join("|", Enum.GetNames<ETargetPlatform>())}]",
        $"-Configuration=[{string.Join('|', Enum.GetNames<ECompileConfiguration>())}]",
        $"-Arch=[{string.Join('|', Enum.GetNames<ETargetArch>())}]"
    );

    public readonly object _threadSafeLock = new();

    public bool Execute(IReadOnlyDictionary<string, ICommandLineArgument> Arguments)
    {
        string ProjectName = Arguments.GetArgumentValue<string>("Project", true) ?? "";
        string PlatformString = Arguments.GetArgumentValue<string>("Platform", true) ?? "";
        string ConfigurationString = Arguments.GetArgumentValue<string>("Configuration", true) ?? "";
        string ArchString = Arguments.GetArgumentValue<string>("Arch") ?? "";

        string[] Modules = Arguments.GetArrayArgument<string>("Modules");

        bool bRecompile = Arguments.ContainsKey("Recompile");
        bool bPrintCompileCommands = Arguments.ContainsKey("PrintCompileCommands");
        bool bPrintLinkCommands = Arguments.ContainsKey("PrintLinkCommands");

        ETargetPlatform CompilePlatform = PlatformString.ToEnum<ETargetPlatform>();
        ECompileConfiguration CompileConfiguration = ConfigurationString.ToEnum<ECompileConfiguration>();
        if (!ArchString.TryToEnum(out ETargetArch CompileArch))
        {
            CompileArch = ETargetArch.x64; // TODO: replace this with host arch
        }

        DirectoryReference RootDirectory = Environment.CurrentDirectory;
        ProjectFinder.CreateAndCompileProject(RootDirectory, ProjectName);

        AHostPlatform HostPlatform = AHostPlatform.GetHost();
        if (!HostPlatform.SupportedTargetPlatforms.TryGetValue(CompilePlatform, out ATargetPlatform? TargetPlatform)) throw new TargetPlatformNotSupportedException(HostPlatform, CompilePlatform);

        ATargetPlatform.TargetPlatform = TargetPlatform;

        AProjectDefinition Project = ProjectFinder.FindProject(ProjectName);
        ProjectDirectories.Create(Project, TargetPlatform, CompileConfiguration);

        if (bRecompile)
        {
            Clean Clean = new();
            Clean.Execute(Arguments);
        }

        // must have all modules here, not only the selected ones due to dependency
        Dictionary<string, AModuleDefinition> AllModulesMap = [];
        AllModulesMap.AddFrom(Project.GetModules(ETargetPlatform.Any), Project.GetModules(TargetPlatform.Platform));

        AModuleDefinition[] AllModules = [.. AllModulesMap.Values];

        AModuleDefinition[] SelectedModules;
        if (Modules is null || Modules.Length == 0)
        {
            SelectedModules = AllModules;
            Console.WriteLine($"WARNING: No module specified, will compile all: {string.Join(", ", AllModulesMap.Keys)}");
        }
        else if (Modules.Length == 1)
        {
            string ModuleName = Modules[0];

            if (!AllModulesMap.TryGetValue(ModuleName, out AModuleDefinition? ModuleDefinition))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Module {ModuleName} not found in project {Project.Name}");
                Console.ResetColor();
                return false;
            }

            // TODO: fix this so specifying a module that has more than one layer of dependencies will not
            // cause a forever waiting linkage from dependencies
            SelectedModules = [
                ModuleDefinition,
                .. ModuleDefinition.GetDependencies(ETargetPlatform.Any),
                .. ModuleDefinition.GetDependencies(TargetPlatform.Platform)
            ];
            Console.WriteLine($"Compiling specified module: {ModuleName}");
        }
        else
        {
            SelectedModules = [.. AllModules.Where(Each => Modules.Contains(Each.Name))];
            Console.WriteLine($"Compiling specified modules: {string.Join(", ", Modules)}");
        }

        Console.WriteLine($"Compiling on {HostPlatform.Name} platform targeting {TargetPlatform.Name}");

        Dictionary<AModuleDefinition, CompileModuleInfo> ModuleCompilationResultMap = AllModules.ToDictionary(
            Module => Module,
            Module =>
            {
                ISourceCollection SC = ISourceCollection.CreateSourceCollection(TargetPlatform.Platform, Module.BinaryType);
                return new CompileModuleInfo(Module, SC, new(Module, TargetPlatform.Toolchain));
            });

        // needs to be sorted for the single thread run
        // otherwise, it will get stuck due to waiting on dependencies
        List<CompileModuleInfo> Sorted = [.. SelectedModules.Select(Module => ModuleCompilationResultMap[Module])];
        Sorted.Sort((Lhs, Rhs) =>
        {
            HashSet<AModuleDefinition> LhsDeps = [
                .. Lhs.Module.GetDependencies(ETargetPlatform.Any),
                .. Lhs.Module.GetDependencies(TargetPlatform.Platform)
            ];

            HashSet<AModuleDefinition> RhsDeps = [
                .. Rhs.Module.GetDependencies(ETargetPlatform.Any),
                .. Rhs.Module.GetDependencies(TargetPlatform.Platform)
            ];

            if (LhsDeps.Contains(Rhs.Module))
            {
                return 1;
            }
            else if (RhsDeps.Contains(Lhs.Module))
            {
                return -1;
            }
            else
            {
                return 0;
            }
        });

        CompileModuleInfo[] CompileModuleInfos = [.. Sorted];

        ChecksumStorage.Shared.LoadChecksums(TargetPlatform.Platform, CompileConfiguration);

        bool bSuccess = true;
        Parallelization.ForEach(CompileModuleInfos, ModuleInfo =>
        {
            CompileModuleTask CompileTask = new(_threadSafeLock, ModuleInfo, TargetPlatform, CompileConfiguration, CompileArch);
            CompileTask.Compile(bPrintCompileCommands);

            CopyResourcesTask CopyResourcesTask = new(ModuleInfo.Module);
            CopyResourcesTask.Copy();

            CompileModuleInfo Info = ModuleCompilationResultMap[ModuleInfo.Module];

            LinkModuleTask LinkTask = new(_threadSafeLock, ModuleInfo, TargetPlatform, CompileConfiguration, CompileArch);
            LinkTask.Link(ModuleCompilationResultMap, bPrintLinkCommands);

            lock (_threadSafeLock)
            {
                bSuccess &=
                    Info.CompileResult is ECompilationResult.NothingToCompile or ECompilationResult.CompilationSuccess &&
                    Info.LinkResult is ELinkageResult.LinkUpToDate or ELinkageResult.LinkSuccess;
            }

            IAdditionalCompileAction[] AdditionalCompileActions = [
                .. ModuleInfo.Module.GetAdditionalCompileActions(ETargetPlatform.Any),
                .. ModuleInfo.Module.GetAdditionalCompileActions(TargetPlatform.Platform)
            ];
            foreach (IAdditionalCompileAction Action in AdditionalCompileActions)
            {
                bSuccess &= Action.Execute(ModuleInfo, TargetPlatform);
            }
        });

        ChecksumStorage.Shared.SaveChecksums(TargetPlatform.Platform, CompileConfiguration);

        if (bSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Project {Project.Name} compiled successfully");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Project {Project.Name} generated compile errors");
        }
        
        Console.ResetColor();

        return bSuccess;
    }
}

public class TargetPlatformNotSupportedException(AHostPlatform InHostPlatform, ETargetPlatform InTargetPlatform) : ABaseException($"Target platform {InTargetPlatform} not supported on host {InHostPlatform.Name}");