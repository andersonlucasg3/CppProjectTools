using ProjectTools.Extensions;
using ProjectTools.IO;
using ProjectTools.Platforms;
using ProjectTools.Processes;
using ProjectTools.Projects;

namespace ProjectTools.Compilation.Actions;

public class PackageAppBundleCompileAction : IAdditionalCompileAction
{
    private string? _moduleName;

    private string ModuleName { get => _moduleName!; set => _moduleName = value; }

    public FileReference? InfoPlistFile;

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

        CopyBinaryAndAllDependenciesToAppBundle(InModuleInfo, InTargetPlatform, OutputAppBundleFolder);

        if (InfoPlistFile is null || !InfoPlistFile.bExists)
        {
            Log($"Expected a valid info plist file, but got: {InfoPlistFile}", ConsoleColor.Red);

            return false;
        }

        FileReference OutputInfoPlistBinaryFile = OutputAppBundleFolder.CombineFile("Info.plist");

        if (OutputInfoPlistBinaryFile.bExists)
        {
            OutputInfoPlistBinaryFile.Delete();
        }

        Log($"Copying {InfoPlistFile.PlatformPath} to {OutputInfoPlistBinaryFile.PlatformPath}");

        ProcessResult Result = this.Run(["xcrun", "plutil", "-convert", "binary1", InfoPlistFile.PlatformPath.Quoted(), "-o", OutputInfoPlistBinaryFile.PlatformPath.Quoted()]);

        if (!Result.bSuccess)
        {
            Log(Result.StandardError, ConsoleColor.Red);

            return false;
        }

        return true;
    }

    private void CopyBinaryAndAllDependenciesToAppBundle(CompileModuleInfo InModuleInfo, ATargetPlatform InTargetPlatform, DirectoryReference InOutputAppBundleFolder)
    {
        Dictionary<FileReference, FileReference> FromToBinariesCopyDict = new()
        {
            { InModuleInfo.Link.LinkedFile, InOutputAppBundleFolder.CombineFile(InModuleInfo.Link.LinkedFile.Name) }
        };

        AModuleDefinition[] Dependencies = [
            .. InModuleInfo.Module.GetDependencies(ETargetPlatform.Any),
            .. InModuleInfo.Module.GetDependencies(InTargetPlatform.Platform)
        ];

        foreach (AModuleDefinition Dependency in Dependencies)
        {
            LinkAction Link = new(Dependency, InTargetPlatform.Toolchain);
            FromToBinariesCopyDict.Add(Link.LinkedFile, InOutputAppBundleFolder.CombineFile(Link.LinkedFile.Name));
        }

        foreach (KeyValuePair<FileReference, FileReference> Pair in FromToBinariesCopyDict)
        {
            if (Pair.Value.bExists)
            {
                Pair.Value.Delete();
            }

            Log($"Copying {Pair.Key} to {Pair.Value}");

            Pair.Key.CopyTo(Pair.Value);
        }
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