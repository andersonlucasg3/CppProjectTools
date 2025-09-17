using ProjectTools.Compilation;
using ProjectTools.IO;
using ProjectTools.Processes;
using ProjectTools.Projects;
using ProjectTools.Toolchains;

namespace ProjectTools.Tasks;

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