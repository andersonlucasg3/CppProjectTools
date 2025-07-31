namespace Shared.Toolchains;

using IO;
using Projects;
using Processes;
using Exceptions;
using Compilers;
using Compilers.Apple;
using Shared.Platforms;


public class XcodeToolchain : AClangToolchain
{
    public const string IPhoneOSVersionMin = "17.0";
    public const string MacOSVersionMin = "13.4";

    private readonly Dictionary<ETargetPlatform, AppleCompiler> _appleCompilers;
    private readonly MetalCompiler _metalCompiler = new();

    public readonly string DeveloperPath;
    public readonly string MacOSSdkPath;
    public readonly string IPhoneOSSdkPath;
    public readonly string SdkVersion;

    public XcodeToolchain()
    {
        ProcessResult XcodeSelectResult = this.Run(["xcode-select", "-p"]);
        ProcessResult ShowSdkPathResult = this.Run(["xcrun", "--sdk", "macosx", "--show-sdk-path"]);
        ProcessResult IPhoneSdkPathResult = this.Run(["xcrun", "--sdk", "iphoneos", "--show-sdk-path"]);
        ProcessResult ShowSdkVersionResult = this.Run(["xcrun", "--show-sdk-version"]);

        DeveloperPath = XcodeSelectResult.StandardOutput;
        MacOSSdkPath = ShowSdkPathResult.StandardOutput.TrimEnd();
        IPhoneOSSdkPath = IPhoneSdkPathResult.StandardOutput.TrimEnd();
        SdkVersion = ShowSdkVersionResult.StandardOutput;

        if (string.IsNullOrEmpty(DeveloperPath) || string.IsNullOrEmpty(MacOSSdkPath) || string.IsNullOrEmpty(SdkVersion))
        {
            throw new XcodeNotInstalledException();
        }

        _appleCompilers = [];
        _appleCompilers.Add(ETargetPlatform.iOS, new($"-miphoneos-version-min={IPhoneOSVersionMin}", IPhoneOSSdkPath));
        _appleCompilers.Add(ETargetPlatform.macOS, new($"-mmacosx-version-min={MacOSVersionMin}", MacOSSdkPath));
    }

    public override string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo)
    {
        ICompiler Compiler = GetCompiler(InCompileCommandInfo.TargetFile, InCompileCommandInfo.TargetPlatform);

        return Compiler.GetCompileCommandLine(InCompileCommandInfo);
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        ICompiler Compiler = GetCompiler(InLinkCommandInfo);

        return Compiler.GetLinkCommandLine(InLinkCommandInfo);
    }

    public override string GetBinaryTypeExtension(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => ".a",
            EModuleBinaryType.DynamicLibrary => ".dylib",
            EModuleBinaryType.ShaderLibrary => ".metallib",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }

    public override string GetBinaryTypePrefix(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => "lib",
            EModuleBinaryType.DynamicLibrary => "lib",
            EModuleBinaryType.ShaderLibrary => "",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }

    public override string GetObjectFileExtension(FileReference InSourceFile)
    {
        return InSourceFile.Extension switch
        {
            ".metal" => _metalCompiler.GetObjectFileExtension(),
            _ => _appleCompilers[ETargetPlatform.macOS].GetObjectFileExtension()
        };
    }

    public override string[] GetAutomaticModuleCompilerDefinitions(AModuleDefinition InModule, ETargetPlatform InTargetPlatform)
    {
        List<string> CompilerDefinitions = [];

        CompilerDefinitions.Add($"{InModule.Name.ToUpper()}_API=");

        AModuleDefinition[] Dependencies = [
            .. InModule.GetDependencies(ETargetPlatform.Any),
            .. InModule.GetDependencies(InTargetPlatform)
        ];

        foreach (AModuleDefinition Dependency in Dependencies)
        {
            CompilerDefinitions.Add($"{Dependency.Name.ToUpper()}_API=");
        }

        return [.. CompilerDefinitions];
    }

    public void PrintToolchain()
    {
        Console.WriteLine($"Using Xcode Toolchain. Sdk version {SdkVersion}");
    }

    private ICompiler GetCompiler(FileReference InFile, ETargetPlatform InTargetPlatform)
    {
        return InFile.Extension switch
        {
            ".metal" => _metalCompiler,
            _ => _appleCompilers[InTargetPlatform]
        };
    }

    private ICompiler GetCompiler(LinkCommandInfo InInfo)
    {
        return InInfo.Module.BinaryType switch
        {
            EModuleBinaryType.ShaderLibrary => _metalCompiler,
            _ => _appleCompilers[InInfo.TargetPlatform]
        };
    }
}

public class XcodeNotInstalledException : ABaseException;
public class SourceFileExtensionNotSupportedException(string InMessage) : ABaseException(InMessage);
