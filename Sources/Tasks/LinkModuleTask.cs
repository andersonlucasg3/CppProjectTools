using ProjectTools.Compilation;
using ProjectTools.IO;
using ProjectTools.Platforms;
using ProjectTools.Processes;
using ProjectTools.Projects;
using ProjectTools.Toolchains;

namespace ProjectTools.Tasks;

public class LinkModuleTask(object InThreadSafeLock, CompileModuleInfo InInfo, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration, ETargetArch InArch)
{
    private readonly ProjectDirectories _compileDirectories = ProjectDirectories.Shared;

    public void Link(Dictionary<AModuleDefinition, CompileModuleInfo> ModuleCompilationResultMap, bool bPrintLinkCommands)
    {
        AModuleDefinition[] Dependencies = [
            .. InInfo.Module.GetDependencies(InTargetPlatform.Platform),
            .. InInfo.Module.GetDependencies(ETargetPlatform.Any),
        ];

        bool bPendingResults = true;
        do
        {
            Thread.Sleep(1);

            bool bAnyDependencyWaiting = Dependencies.Any(DependencyModule =>
            {
                CompileModuleInfo ModuleInfo = ModuleCompilationResultMap[DependencyModule];
                lock (InThreadSafeLock) return ModuleInfo.CompileResult is ECompilationResult.Waiting && ModuleInfo.LinkResult is ELinkageResult.Waiting;
            });

            bool bStillCompiling = true;
            lock (InThreadSafeLock) bStillCompiling = InInfo.CompileResult is ECompilationResult.Waiting;

            bPendingResults = bAnyDependencyWaiting || bStillCompiling;
        }
        while (bPendingResults);

        bool bCanLink = false;
        do
        {
            bool bAllDependenciesUpToDate = Dependencies.All(DependencyModule =>
            {
                CompileModuleInfo ModuleInfo = ModuleCompilationResultMap[DependencyModule];
                lock (InThreadSafeLock) return ModuleInfo.CompileResult is ECompilationResult.NothingToCompile && ModuleInfo.LinkResult is ELinkageResult.LinkUpToDate;
            });

            bool bAnyDependenciesLinked = Dependencies.Any(DependencyModule =>
            {
                CompileModuleInfo ModuleInfo = ModuleCompilationResultMap[DependencyModule];
                lock (InThreadSafeLock) return ModuleInfo.CompileResult is ECompilationResult.NothingToCompile && ModuleInfo.LinkResult is ELinkageResult.LinkSuccess;
            });

            bool bSelfUpToDate = false;
            bool bSelfCompilationFailure = false;
            lock (InThreadSafeLock)
            {
                bSelfUpToDate = InInfo.CompileResult is ECompilationResult.NothingToCompile;
                bSelfCompilationFailure = InInfo.CompileResult is ECompilationResult.CompilationFailed;
            }

            if (bAllDependenciesUpToDate && bSelfUpToDate && InInfo.Link.LinkedFile.bExists)
            {
                Console.WriteLine($"Link [{InInfo.Module.Name}]: Up to date");

                lock (InThreadSafeLock) InInfo.LinkResult = ELinkageResult.LinkUpToDate;

                return;
            }
            else if (bSelfCompilationFailure)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Link [{InInfo.Module.Name}]: Skipped due to dependency failure");

                lock (InThreadSafeLock) InInfo.LinkResult = ELinkageResult.LinkFailed;

                return;
            }

            bool bAnyDependencyFailed = Dependencies.Any(DependencyModule =>
            {
                CompileModuleInfo ModuleInfo = ModuleCompilationResultMap[DependencyModule];
                lock (InThreadSafeLock) return ModuleInfo.CompileResult is ECompilationResult.CompilationFailed || ModuleInfo.LinkResult is ELinkageResult.LinkFailed;
            });

            if (bAnyDependencyFailed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Link [{InInfo.Module.Name}]: Skipped due to dependency failure");

                lock (InThreadSafeLock) InInfo.LinkResult = ELinkageResult.LinkFailed;

                return;
            }

            bool bAllDependenciesSucceeded = Dependencies.All(DependencyModule =>
            {
                CompileModuleInfo ModuleInfo = ModuleCompilationResultMap[DependencyModule];
                lock (InThreadSafeLock) return ModuleInfo.CompileResult is ECompilationResult.NothingToCompile or ECompilationResult.CompilationSuccess && ModuleInfo.LinkResult is ELinkageResult.LinkUpToDate or ELinkageResult.LinkSuccess;
            });

            bCanLink = bAllDependenciesSucceeded;

            Thread.Sleep(1);
        } 
        while (!bCanLink);

        Console.ResetColor();

        FileReference[] ObjectFiles = [.. InInfo.CompileActions.Select(Action => Action.ObjectFile)];

        DirectoryReference[] LibrarySearchPaths = [
            _compileDirectories.CreateBaseConfigurationDirectory(ECompileBaseDirectory.Binaries),
            .. InInfo.Module.LibrarySearchPaths
        ];

        string[] LinkWithLibraries = [.. InInfo.Module.GetLinkWithLibraries(InTargetPlatform.Platform)];
        
        LinkCommandInfo LinkCommandInfo = new()
        {
            Platform = InTargetPlatform.Platform,
            Module = InInfo.Module,
            LinkedFile = InInfo.Link.LinkedFile,
            LibrarySearchPaths = LibrarySearchPaths,
            ObjectFiles = ObjectFiles,
            TargetPlatform = InTargetPlatform.Platform,
            Configuration = InConfiguration,
            Arch = InArch,
            LinkWithLibraries = LinkWithLibraries,
        };
        
        lock (InThreadSafeLock)
        {
            Console.WriteLine($"Link [{InInfo.Module.Name}]: {InInfo.Link.LinkedFile.Name}");
            if (bPrintLinkCommands)
            {
                string[] CommandLine = InTargetPlatform.Toolchain.GetLinkCommandLine(LinkCommandInfo);
                Console.WriteLine($"    INFO: {string.Join(' ', CommandLine)}");
            }
        }

        ProcessResult LinkResult = InTargetPlatform.Toolchain.Link(LinkCommandInfo);

        lock (InThreadSafeLock)
        {
            if (LinkResult.bSuccess)
            {
                InInfo.LinkResult = ELinkageResult.LinkSuccess;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Link [{InInfo.ModuleName}]: {InInfo.Link.LinkedFile.Name}");
            }
            else
            {
                InInfo.LinkResult = ELinkageResult.LinkFailed;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Link [{InInfo.ModuleName}]: {InInfo.Link.LinkedFile.Name}{Environment.NewLine}{LinkResult.StandardError}");
            }

            Console.ResetColor();
        }
    }
}
