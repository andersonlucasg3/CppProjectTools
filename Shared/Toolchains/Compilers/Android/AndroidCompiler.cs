namespace Shared.Toolchains.Compilers.Android;

using IO;
using Projects;
using Compilation;
using Shared.Extensions;

public class AndroidCompiler(DirectoryReference InPrebuiltPlatformRoot, string InAArch, int InMinimumSupportedAndroidNdkVersion) : ACppCompiler
{
    private readonly FileReference _clangCompiler = InPrebuiltPlatformRoot.CombineFile("bin", "clang");
    private readonly FileReference _clangPlusPlusCompiler = InPrebuiltPlatformRoot.CombineFile("bin", "clang++");

    public override string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo)
    {
        return [
            GetClangBySourceExtension(InCompileCommandInfo.TargetFile.Extension),
            "-MMD",
            "-MF",
            InCompileCommandInfo.DependencyFile.PlatformPath.Quoted(),
            "-c",
            InCompileCommandInfo.TargetFile.PlatformPath.Quoted(),
            "-o",
            InCompileCommandInfo.ObjectFile.PlatformPath.Quoted(),
            .. GetSystemIncludePaths().Select(Path => $"-I{Path.PlatformPath.Quoted()}"),
            $"-I{InCompileCommandInfo.SourcesDirectory.PlatformPath.Quoted()}",
            .. InCompileCommandInfo.HeaderSearchPaths.Select(IncludeDirectory => $"-I{IncludeDirectory.PlatformPath.Quoted()}"),
            "-fPIC",
            $"-std={CppStandard}",
            "-stdlib=libc++",
            "-Wall",
            "-Wextra",
            $"--target={InAArch}-linux-android{InMinimumSupportedAndroidNdkVersion}",
            $"--sysroot={InPrebuiltPlatformRoot.Combine("sysroot").PlatformPath.Quoted()}",
            .. InCompileCommandInfo.CompilerDefinitions.Select(Define => $"-D{Define}"),
            .. GetOptimizationArguments(InCompileCommandInfo.Configuration),
        ];
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        return [
            _clangPlusPlusCompiler.PlatformPath,
            .. GetBinaryTypeArguments(InLinkCommandInfo.Module.BinaryType),
            string.Join(' ', InLinkCommandInfo.ObjectFiles.Select(Each => Each.PlatformPath.Quoted())),
            "-o",
            InLinkCommandInfo.LinkedFile.PlatformPath.Quoted(),
            .. GetSystemLibrarySearchPaths().Select(Each => $"-L{Each.PlatformPath.Quoted()}"),
            .. InLinkCommandInfo.LibrarySearchPaths.Select(LibrarySearchPath => $"-L{LibrarySearchPath.PlatformPath.Quoted()}"),
            .. InLinkCommandInfo.Module.GetDependencies().Select(Dependency => $"-l{Dependency.Name}"),
            $"--target={InAArch}-linux-android{InMinimumSupportedAndroidNdkVersion}",
            "-llog", "-landroid", "-lc++", "-lc",
        ];
    }

    public override string GetObjectFileExtension()
    {
        return ".o";
    }

    private DirectoryReference[] GetSystemIncludePaths()
    {
        DirectoryReference SysrootInclude = InPrebuiltPlatformRoot.Combine("sysroot", "usr", "include");
        return [
            SysrootInclude,
            SysrootInclude.Combine("c++", "v1"),
            InPrebuiltPlatformRoot.Combine("include", "c++", "v1")
        ];
    }

    private DirectoryReference[] GetSystemLibrarySearchPaths()
    {
        DirectoryReference SysrootLibraries = InPrebuiltPlatformRoot.Combine("sysroot", "usr", "lib", $"{InAArch}-linux-android");
        return [
            SysrootLibraries,
            SysrootLibraries.Combine($"{InMinimumSupportedAndroidNdkVersion}"),
        ];
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
        if (CCompiledSourceExtensions.Contains(FileExtension)) return _clangCompiler.PlatformPath;
        if (CppCompiledSourceExtensions.Contains(FileExtension)) return _clangPlusPlusCompiler.PlatformPath;

        throw new SourceFileExtensionNotSupportedException(FileExtension);
    }

    private static string[] GetBinaryTypeArguments(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => [ "-fPIE", "-pie", ],
            EModuleBinaryType.StaticLibrary => [ "-static", "-fPIC" ],
            EModuleBinaryType.DynamicLibrary => [ "-shared", "-fPIC" ],
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }
}