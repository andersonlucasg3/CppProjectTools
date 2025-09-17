using ProjectTools.Compilation;
using ProjectTools.IO;
using ProjectTools.Platforms;
using ProjectTools.Processes;
using ProjectTools.Projects;
using ProjectTools.Sources;
using ProjectTools.Toolchains;

namespace ProjectTools.Tasks;

public class CompileModuleTask(object InThreadSafeLock, CompileModuleInfo InInfo, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration, ETargetArch InArch)
{
    public void Compile(bool bPrintCompileCommands)
    {
        ISourceCollection SourceCollection = InInfo.SourceCollection;

        SourceCollection.GatherSourceFiles(InInfo.Module.SourcesDirectory);

        CompileAction[] SourceCompileActions = GenerateCompileActions(InTargetPlatform.Toolchain, SourceCollection, out InInfo.CompileActions);
        
        if (SourceCompileActions.Length == 0)
        {
            Console.WriteLine($"Nothing to compile, module {InInfo.ModuleName} is up to date.");

            InInfo.CompileResult = ECompilationResult.NothingToCompile;
            
            return;
        }
            
        int CompileActionCount = SourceCompileActions.Length;
        
        Console.WriteLine($"Compiling module {InInfo.ModuleName} with {CompileActionCount} actions");
        
        bool bCompilationSuccessful = true;

        int Index = 0;
        Parallelization.ForEach(SourceCompileActions, InAction =>
        {
            lock (InThreadSafeLock)
            {
                Console.WriteLine($"Compile [{InInfo.ModuleName}]: {InAction.SourceFile.Name}");
                if (bPrintCompileCommands)
                {
                    string[] CommandLine = InTargetPlatform.Toolchain.GetCompileCommandline(InAction.CompileCommandInfo);
                    Console.WriteLine($"    INFO: {string.Join(' ', CommandLine)}");
                }
            }
            
            ProcessResult CompileResult = InTargetPlatform.Toolchain.Compile(InAction.CompileCommandInfo);

            if (CompileResult.bSuccess)
            {
                ChecksumStorage.Shared.CompilationSuccess(InAction, InTargetPlatform.Toolchain);
            }
            else
            {
                ChecksumStorage.Shared.CompilationFailed(InAction);
                
                bCompilationSuccessful = false;
            }

            lock (InThreadSafeLock)
            {
                if (CompileResult.bSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Compile [{++Index}/{CompileActionCount}] [{InInfo.ModuleName}]: {InAction.SourceFile.Name}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Compile [{++Index}/{CompileActionCount}] [{InInfo.ModuleName}]: {InAction.SourceFile.Name}{Environment.NewLine}{CompileResult.StandardError}");
                }

                Console.ResetColor();
            }
        });

        lock (InThreadSafeLock)
        {
            InInfo.CompileResult = bCompilationSuccessful ? ECompilationResult.CompilationSuccess : ECompilationResult.CompilationFailed;
        }
    }

    private CompileAction[] GenerateCompileActions(IToolchain InToolchain, ISourceCollection InSourceCollection, out CompileAction[] OutCompileActions)
    {
        ProjectDirectories Directories = ProjectDirectories.Shared;
        
        DirectoryReference ObjectsDirectory = Directories.CreateIntermediateObjectsDirectory(InInfo.ModuleName);

        List<CompileAction> FullCompileActionsList = [];
        List<CompileAction> FilteredCompileActionList = [];
        
        Parallelization.ForEach(InSourceCollection.SourceFiles, SourceFile =>
        {
            CompileAction SourceCompileAction = new(InInfo.Module, InTargetPlatform, InConfiguration, InArch, SourceFile, ObjectsDirectory, InSourceCollection);

            lock (InThreadSafeLock)
            {
                FullCompileActionsList.Add(SourceCompileAction);

                if (ChecksumStorage.Shared.ShouldRecompile(SourceCompileAction, InToolchain))
                {
                    FilteredCompileActionList.Add(SourceCompileAction);
                }
            }
        });
        OutCompileActions = [.. FullCompileActionsList];
        
        return [.. FilteredCompileActionList];
    }
}
