using Shared.Compilation;
using Shared.IO;
using Shared.Processes;
using Shared.Projects;
using Shared.Toolchains;

namespace BuildTool.Compilation;

public class CleanTask(AProjectDefinition InProjectDefinition, AModuleDefinition[] InModules, IToolchain InToolchain)
{
    private readonly ProjectDirectories _compileDirectories = ProjectDirectories.Shared;
    
    public void Clean()
    {
        Parallelization.ForEach(InModules, Module =>
        {
            DirectoryReference BinaryConfigurationDirectory = _compileDirectories.CreateBaseConfigurationDirectory(ECompileBaseDirectory.Binaries, false);

            if (BinaryConfigurationDirectory.bExists)
            {
                LinkAction Linkage = new(Module, InToolchain);
                if (Linkage.LinkedFile.bExists) Linkage.LinkedFile.Delete();
            }

            DirectoryReference ModuleDirectory = _compileDirectories.CreateModuleDirectory(ECompileBaseDirectory.Intermediate, Module.Name, false);

            if (!ModuleDirectory.bExists) return;
            
            Console.WriteLine($"Cleaning {InProjectDefinition.Name}'s Intermediate for module: {Module.Name}");

            ModuleDirectory.Delete(true);
        });
    }
}