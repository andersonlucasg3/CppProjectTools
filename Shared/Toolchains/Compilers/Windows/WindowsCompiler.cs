using Shared.Compilation;
using Shared.Extensions;
using Shared.IO;
using Shared.Projects;

namespace Shared.Toolchains.Compilers.Windows;

public class WindowsCompiler(string InClangPath, string InLinkPath) : ACppCompiler
{
    private readonly string _clangPath = InClangPath;
    private readonly string _linkPath = InLinkPath;

    public override string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo)
    {
        return [
            _clangPath,
            "/showIncludes",
            "/c",
            InCompileCommandInfo.TargetFile.PlatformPath,
            $"/I{InCompileCommandInfo.SourcesDirectory.PlatformPath}",
            .. InCompileCommandInfo.HeaderSearchPaths.Select(IncludeDirectory => $"/I{IncludeDirectory.PlatformPath}"),
            $"/Fo{InCompileCommandInfo.ObjectFile.PlatformPath}",
            "/std:c++20",
            "/W4",
            "/EHsc",
            "/GR",
            .. InCompileCommandInfo.CompilerDefinitions.Select(Define => $"/D{Define}"),
            .. GetCompilerOptimizationArguments(InCompileCommandInfo.Configuration),
        ];
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        List<string> CommandLine = [
            _linkPath,
            .. InLinkCommandInfo.ObjectFiles.Select(ObjectFile => ObjectFile.PlatformPath.Quoted()),
            GetLinkArgumentForBinaryType(InLinkCommandInfo.Module.BinaryType),
            $"/OUT:{InLinkCommandInfo.LinkedFile.PlatformPath.Quoted()}",
            .. InLinkCommandInfo.LinkWithLibraries.Select(LinkLibrary => $"/defaultlib:{LinkLibrary}"),
            .. GetLinkerOptimizationArguments(InLinkCommandInfo.Configuration, InLinkCommandInfo.LinkedFile),
        ];

        if (InLinkCommandInfo.LibrarySearchPaths.Length > 0)
        {
            CommandLine.AddRange(InLinkCommandInfo.LibrarySearchPaths.Select(LibrarySearchPath => $"/LIBPATH:{LibrarySearchPath.PlatformPath.Quoted()}"));
        }

        AModuleDefinition[] ModuleDependencies = [
            .. InLinkCommandInfo.Module.GetDependencies(Platforms.ETargetPlatform.Any),
            .. InLinkCommandInfo.Module.GetDependencies(InLinkCommandInfo.TargetPlatform)
        ];
        if (ModuleDependencies.Length > 0)
        {
            CommandLine.AddRange(ModuleDependencies.Select(Dependency => $"{Dependency.Name}.lib"));
        }

        return [.. CommandLine];
    }

    public override string GetObjectFileExtension()
    {
        return ".obj";
    }

    private static string[] GetCompilerOptimizationArguments(ECompileConfiguration InConfiguration)
    {
        return InConfiguration switch
        {
            ECompileConfiguration.Debug => ["/DDEBUG", "/Zi"],
            ECompileConfiguration.Release => ["/flto", "/O3", "/DNDEBUG"],
            _ => throw new ArgumentOutOfRangeException(nameof(InConfiguration), InConfiguration, null)
        };
    }

    private static string[] GetLinkerOptimizationArguments(ECompileConfiguration InConfiguration, FileReference InLinkedFile)
    {
        return InConfiguration switch
        {
            ECompileConfiguration.Debug => ["/DEBUG", $"/PDB:{InLinkedFile.ChangeExtension("pdb").PlatformPath.Quoted()}"],
            ECompileConfiguration.Release => [], // maybe in the future?
            _ => throw new ArgumentOutOfRangeException(nameof(InConfiguration), InConfiguration, null),
        };
    }

    private static string GetLinkArgumentForBinaryType(EModuleBinaryType InBinaryType)
    {
        return InBinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => throw new NotSupportedException($"{InBinaryType}"),
            EModuleBinaryType.DynamicLibrary => "/DLL",
            EModuleBinaryType.ShaderLibrary => throw new ShaderLibraryNotSupportedOnPlatformException(InBinaryType),
            _ => throw new ArgumentOutOfRangeException(nameof(InBinaryType), InBinaryType, null),
        };
    }
}