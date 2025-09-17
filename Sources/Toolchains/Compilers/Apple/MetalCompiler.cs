namespace ProjectTools.Toolchains.Compilers.Apple;

using Compilation;

public class MetalCompiler : ICompiler
{
    public string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo)
    {
        return [
            "xcrun",
            "-sdk",
            "macosx",
            "metal",
            InCompileCommandInfo.TargetFile.PlatformPath,
            "-o",
            InCompileCommandInfo.ObjectFile.PlatformPath,
            "-c",
            "-fembed-bitcode",
            .. GetOptimizationArguments(InCompileCommandInfo.Configuration),
        ];
    }

    public string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        return [
            "xcrun",
            "-sdk",
            "macosx",
            "metallib",
            string.Join(' ', InLinkCommandInfo.ObjectFiles.Select(Each => Each.PlatformPath)),
            "-o",
            InLinkCommandInfo.LinkedFile.PlatformPath,
        ];
    }

    public string GetObjectFileExtension()
    {
        return ".air";
    }

    private static string[] GetOptimizationArguments(ECompileConfiguration InConfiguration)
    {
        return InConfiguration switch
        {
            ECompileConfiguration.Debug => ["-g", "-O0"],
            ECompileConfiguration.Release => ["-O3"],
            _ => throw new ArgumentOutOfRangeException(nameof(InConfiguration), InConfiguration, null)
        };
    }
}