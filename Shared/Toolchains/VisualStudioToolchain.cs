using System.Text.RegularExpressions;

namespace Shared.Toolchains;

using Compilation;
using Exceptions;
using IO;
using Processes;
using Projects;
using Compilers.Windows;
using Platforms;
using Shared.Extensions;

public partial class VisualStudioToolchain : AClangToolchain
{
    private readonly string _vsToolchainRoot;
    private readonly string _clangPath;
    private readonly string _linkPath;

    private readonly WindowsCompiler _windowsCompiler;

    public VisualStudioToolchain()
    {
        if (!TryGetVSWhereExecutablePath(out string VSWherePath))
        {
            ProcessResult Result = this.Run(["pwsh", "-c", "winget", "install", "Microsoft.VisualStudio.Locator"]);

            if (!Result.bSuccess) throw new VisualStudioToolchainNotFoundException(Result.StandardError);
        }

        _vsToolchainRoot = GetVisualStudioToolchainPath(VSWherePath);
        _clangPath = Path.Combine(_vsToolchainRoot, "VC", "Tools", "Llvm", "x64", "bin", "clang-cl.exe").Quoted();
        _linkPath = Path.Combine(_vsToolchainRoot, "VC", "Tools", "Llvm", "x64", "bin", "lld-link.exe").Quoted();

        _windowsCompiler = new(_clangPath, _linkPath);
    }

    public override string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo)
    {
        return _windowsCompiler.GetCompileCommandLine(InCompileCommandInfo);
    }

    public override ProcessResult Compile(CompileCommandInfo InCompileCommandInfo)
    {
        ProcessResult Result = base.Compile(InCompileCommandInfo);

        if (Result.bSuccess)
        {
            Regex Regex = GetHeaderRegex();

            FileReference[] HeaderFiles = [.. Result.StandardOutput.Split(Environment.NewLine).SelectMany(Line => Regex.Matches(Line).Select(Match => Match.Groups[1].Value))];

            CompileDependency Dependency = new(InCompileCommandInfo.TargetFile, InCompileCommandInfo.ObjectFile, HeaderFiles);

            Dependency.WriteToFile(InCompileCommandInfo.DependencyFile);
        }

        return Result;
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        return _windowsCompiler.GetLinkCommandLine(InLinkCommandInfo);
    }

    public override string GetBinaryTypeExtension(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => ".exe",
            EModuleBinaryType.StaticLibrary => ".lib",
            EModuleBinaryType.DynamicLibrary => ".dll",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }

    public override string GetBinaryTypePrefix(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => "",
            EModuleBinaryType.DynamicLibrary => "",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }

    public override string GetObjectFileExtension(FileReference InSourceFile)
    {
        return _windowsCompiler.GetObjectFileExtension();
    }

    public override string[] GetAutomaticModuleCompilerDefinitions(AModuleDefinition InModule, ETargetPlatform InTargetPlatform)
    {
        List<string> CompilerDefinitions = [];

        CompilerDefinitions.Add($"{InModule.Name.ToUpper()}_API=__declspec(dllexport)".Quoted());

        AModuleDefinition[] Dependencies = [
            .. InModule.GetDependencies(ETargetPlatform.Any),
            .. InModule.GetDependencies(InTargetPlatform)
        ];

        foreach (AModuleDefinition Dependency in Dependencies)
        {
            CompilerDefinitions.Add($"{Dependency.Name.ToUpper()}_API=__declspec(dllimport)".Quoted());
        }

        return [.. CompilerDefinitions];
    }

    private string GetVisualStudioToolchainPath(string InVSWherePath)
    {
        ProcessResult Result = this.Run([
            InVSWherePath,
            "-latest",
            "-products",
            "*",
            "-requires",
            "Microsoft.VisualStudio.Component.VC.Tools.x86.x64",
            "-property",
            "installationPath"
        ]);

        if (!Result.bSuccess) throw new VisualStudioToolchainNotFoundException();

        return Result.StandardOutput.Trim();
    }

    private static bool TryGetVSWhereExecutablePath(out string OutVSWherePath)
    {
        string? ProgramFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

        if (ProgramFilesX86 is null)
        {
            OutVSWherePath = "";
            return false;
        }

        OutVSWherePath = Path.Combine(ProgramFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
        return File.Exists(OutVSWherePath);
    }

    [GeneratedRegex("file:\\s+([a-zA-Z0-9:\\s+\\\\.\\/\\(\\)_]+)")]
    private static partial Regex GetHeaderRegex();
}

public class VisualStudioToolchainNotFoundException : ABaseException
{
    public VisualStudioToolchainNotFoundException() : base() { }
    public VisualStudioToolchainNotFoundException(string InMessage) : base(InMessage) { }
}