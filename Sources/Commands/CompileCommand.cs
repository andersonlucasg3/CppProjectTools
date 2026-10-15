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

            SelectedModules = CollectWithTransitiveDependencies([ModuleDefinition], TargetPlatform.Platform);
            Console.WriteLine($"Compiling specified module: {ModuleName}");
        }
        else
        {
            AModuleDefinition[] Requested = [.. AllModules.Where(Each => Modules.Contains(Each.Name))];
            SelectedModules = CollectWithTransitiveDependencies(Requested, TargetPlatform.Platform);
            Console.WriteLine($"Compiling specified modules: {string.Join(", ", Modules)}");
        }

        Console.WriteLine($"Compiling transitive closure: {string.Join(", ", SelectedModules.Select(m => m.Name))}");

        Console.WriteLine($"Compiling on {HostPlatform.Name} platform targeting {TargetPlatform.Name}");

        Dictionary<AModuleDefinition, CompileModuleInfo> ModuleCompilationResultMap = AllModules.ToDictionary(
            Module => Module,
            Module =>
            {
                string[] ExcludedSourcePaths = [
                    .. Module.GetExcludedSourcePaths(ETargetPlatform.Any),
                    .. Module.GetExcludedSourcePaths(TargetPlatform.Platform)
                ];

                ISourceCollection SC = ISourceCollection.CreateSourceCollection(TargetPlatform.Platform, Module.BinaryType, ExcludedSourcePaths);
                return new CompileModuleInfo(Module, SC, new(Module, TargetPlatform.Toolchain));
            });

        // Topological sort: dependencies must appear before dependents.
        // Uses Kahn's algorithm to produce a valid compilation order.
        AModuleDefinition[] SortedModules = TopologicalSort(SelectedModules, TargetPlatform.Platform);
        List<CompileModuleInfo> Sorted = [.. SortedModules.Select(Module => ModuleCompilationResultMap[Module])];

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

    /// <summary>
    /// Returns the full transitive closure of dependencies for the given seed modules.
    /// </summary>
    private static AModuleDefinition[] CollectWithTransitiveDependencies(AModuleDefinition[] Seeds, ETargetPlatform Platform)
    {
        HashSet<AModuleDefinition> Closure = [];
        Queue<AModuleDefinition> Queue = new(Seeds);

        while (Queue.TryDequeue(out AModuleDefinition? Module))
        {
            if (!Closure.Add(Module)) continue;

            foreach (AModuleDefinition Dep in Module.GetDependencies(ETargetPlatform.Any))
                Queue.Enqueue(Dep);
            foreach (AModuleDefinition Dep in Module.GetDependencies(Platform))
                Queue.Enqueue(Dep);
        }

        return [.. Closure];
    }

    /// <summary>
    /// Produces a valid compilation order using Kahn's topological sort.
    /// Dependencies are guaranteed to appear before the modules that depend on them.
    /// </summary>
    private static AModuleDefinition[] TopologicalSort(AModuleDefinition[] Modules, ETargetPlatform Platform)
    {
        // Build adjacency and in-degree maps
        Dictionary<AModuleDefinition, int> InDegree = Modules.ToDictionary(m => m, _ => 0);
        Dictionary<AModuleDefinition, List<AModuleDefinition>> Dependents = Modules.ToDictionary(m => m, _ => new List<AModuleDefinition>());

        foreach (AModuleDefinition Module in Modules)
        {
            foreach (AModuleDefinition Dep in Module.GetDependencies(ETargetPlatform.Any))
            {
                if (InDegree.ContainsKey(Dep) && Dependents.ContainsKey(Dep))
                {
                    Dependents[Dep].Add(Module);
                    InDegree[Module]++;
                }
            }
            foreach (AModuleDefinition Dep in Module.GetDependencies(Platform))
            {
                if (InDegree.ContainsKey(Dep) && Dependents.ContainsKey(Dep))
                {
                    Dependents[Dep].Add(Module);
                    InDegree[Module]++;
                }
            }
        }

        // Kahn's algorithm
        Queue<AModuleDefinition> Queue = new(InDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        List<AModuleDefinition> Sorted = [];

        while (Queue.TryDequeue(out AModuleDefinition? Module))
        {
            Sorted.Add(Module);

            foreach (AModuleDefinition Dependent in Dependents[Module])
            {
                InDegree[Dependent]--;
                if (InDegree[Dependent] == 0)
                    Queue.Enqueue(Dependent);
            }
        }

        // Any remaining modules form cycles — append them at the end
        foreach (AModuleDefinition Module in InDegree.Where(kv => kv.Value > 0).Select(kv => kv.Key))
        {
            Sorted.Add(Module);
        }

        return [.. Sorted];
    }
}

public class TargetPlatformNotSupportedException(AHostPlatform InHostPlatform, ETargetPlatform InTargetPlatform) : ABaseException($"Target platform {InTargetPlatform} not supported on host {InHostPlatform.Name}");