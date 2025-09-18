using ProjectTools.IO;
using ProjectTools.Platforms;
using ProjectTools.Projects;

namespace ProjectTools.Compilation.Actions;

public class PackageAppBundleCompileAction : IAdditionalCompileAction
{
    private string? _moduleName;

    private string ModuleName { get => _moduleName!; set => _moduleName = value; }

    public bool Execute(CompileModuleInfo InModuleInfo, ATargetPlatform InTargetPlatform)
    {
        ModuleName = InModuleInfo.ModuleName;
        string BundleFolderName = $"{ModuleName}.app";

        Log(ModuleName);

        if (InModuleInfo.LinkResult is ELinkageResult.LinkFailed)
        {
            Log("Link failed, cannot package app", ConsoleColor.Red);

            return false;
        }

        AModuleDefinition Module = InModuleInfo.Module;

        DirectoryReference BinariesDirectory = ProjectDirectories.Shared.CreateBaseConfigurationDirectory(ECompileBaseDirectory.Binaries);
        DirectoryReference OutputAppBundleFolder = BinariesDirectory.Combine(BundleFolderName);

        if (!OutputAppBundleFolder.bExists)
        {
            Log($"Creating {BundleFolderName} folder");

            OutputAppBundleFolder.Create();
        }

        FileReference OutputBinaryFile = OutputAppBundleFolder.CombineFile(InModuleInfo.Link.LinkedFile.Name);

        Log($"Copying binary {InModuleInfo.Link.LinkedFile} to {OutputBinaryFile}");
        
        InModuleInfo.Link.LinkedFile.CopyTo(OutputBinaryFile);

        return true;
    }

    private void Log(string InMessage, ConsoleColor? Color = null)
    {
        if (Color is not null)
        {
            Console.ForegroundColor = Color ?? ConsoleColor.White;
        }

        Console.WriteLine($"Package [{ModuleName}]: {InMessage}");

        if (Color is not null)
        {
            Console.ResetColor();
        }
    }
}