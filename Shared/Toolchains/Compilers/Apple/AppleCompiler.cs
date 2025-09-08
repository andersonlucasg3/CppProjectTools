namespace Shared.Toolchains.Compilers.Apple;

using Sources;
using Projects;
using Compilation;
using Shared.IO;
using Shared.Projects.Platforms.Apple;
using Shared.Extensions;


public class AppleCompiler(string InTargetOSVersionMin, string InSdkPath) : ACppCompiler
{
    public override string[] CCompiledSourceExtensions => [.. base.CCompiledSourceExtensions, .. AppleSourceCollection.ObjCSourceFilesExtensions];
    public override string[] CppCompiledSourceExtensions => [.. base.CppCompiledSourceExtensions, .. AppleSourceCollection.ObjCppSourceFilesExtensions];

    public override string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo)
    {
        return [
            "xcrun",
            GetClangBySourceExtension(InCompileCommandInfo.TargetFile.Extension),
            .. GetLanguageBySourceExtension(InCompileCommandInfo.TargetFile.Extension),
            "-MMD",
            "-MF",
            InCompileCommandInfo.DependencyFile.PlatformPath,
            $"-I{InCompileCommandInfo.SourcesDirectory}",
            .. InCompileCommandInfo.HeaderSearchPaths.Select(IncludeDirectory => $"-I{IncludeDirectory}"),
            "-fPIC",
            $"-std={CppStandard}",
            "-stdlib=libc++",
            "-fno-objc-exceptions",
            "-Wall",
            "-Wextra",
            .. InCompileCommandInfo.CompilerDefinitions.Select(Define => $"-D{Define}"),
            .. GetOptimizationArguments(InCompileCommandInfo.Configuration),
            InTargetOSVersionMin,
            "-isysroot",
            InSdkPath,
            "-c",
            InCompileCommandInfo.TargetFile.PlatformPath,
            "-o",
            InCompileCommandInfo.ObjectFile.PlatformPath,
        ];
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        List<string> CommandLine = [
            "xcrun",
            "clang++",
            GetClangBinaryTypeArgument(InLinkCommandInfo.Module.BinaryType),
            string.Join(' ', InLinkCommandInfo.ObjectFiles.Select(Each => Each.PlatformPath)),
            "-o",
            InLinkCommandInfo.LinkedFile.PlatformPath,
            "-rpath",
            "@loader_path/",
        ];

        if (InLinkCommandInfo.Module.BinaryType == EModuleBinaryType.DynamicLibrary)
        {
            CommandLine.AddRange([
                "-install_name",
                $"@rpath/{InLinkCommandInfo.LinkedFile.Name}"
            ]);
        }

        if (InLinkCommandInfo.LibrarySearchPaths.Length > 0)
        {
            CommandLine.AddRange(InLinkCommandInfo.LibrarySearchPaths.Select(LibrarySearchPath => $"-L{LibrarySearchPath}"));
        }

        AModuleDefinition[] ModuleDependencies = [
            .. InLinkCommandInfo.Module.GetDependencies(Platforms.ETargetPlatform.Any),
            .. InLinkCommandInfo.Module.GetDependencies(InLinkCommandInfo.TargetPlatform)
        ];
        if (ModuleDependencies.Length > 0)
        {
            CommandLine.AddRange(ModuleDependencies.Select(Dependency => $"-l{Dependency.Name}"));
        }

        ApplePlatformSpecifics Specifics = InLinkCommandInfo.Module.PlatformSpecifics.Get(InLinkCommandInfo.TargetPlatform);

        foreach (DirectoryReference SearchPath in Specifics.FrameworkSearchPaths)
        {
            CommandLine.Add($"-F{SearchPath.PlatformPath.Quoted()}");
        }

        foreach (string Framework in Specifics.FrameworkDependencies)
        {
            CommandLine.Add("-framework");
            CommandLine.Add(Framework);
        }

        CommandLine.Add("-ObjC");
        CommandLine.Add("-lobjc");

        CommandLine.Add(InTargetOSVersionMin);

        return [.. CommandLine];
    }

    public override string GetObjectFileExtension()
    {
        return ".o";
    }

    private string[] GetLanguageBySourceExtension(string FileExtension)
    {
        if (CCompiledSourceExtensions.Contains(FileExtension))
        {
            return ["-x", "objective-c"];
        }

        if (CppCompiledSourceExtensions.Contains(FileExtension))
        {
            return ["-x", "objective-c++"];
        }

        return [];
    }

    private static string[] GetOptimizationArguments(ECompileConfiguration InConfiguration)
    {
        return InConfiguration switch
        {
            ECompileConfiguration.Debug => ["-O0", "-DDEBUG", "-g"],
            ECompileConfiguration.Release => ["-flto", "-O3", "-DNDEBUG"],
            _ => throw new ArgumentOutOfRangeException(nameof(InConfiguration), InConfiguration, null)
        };
    }

    private string GetClangBySourceExtension(string FileExtension)
    {
        if (CCompiledSourceExtensions.Contains(FileExtension)) return "clang";
        if (CppCompiledSourceExtensions.Contains(FileExtension)) return "clang++";

        throw new SourceFileExtensionNotSupportedException(FileExtension);
    }

    private static string GetClangBinaryTypeArgument(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => "-staticlib",
            EModuleBinaryType.DynamicLibrary => "-dynamiclib",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }
}