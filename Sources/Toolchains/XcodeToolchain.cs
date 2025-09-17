namespace ProjectTools.Toolchains;

using IO;
using Projects;
using Processes;
using Exceptions;
using Compilers;
using Compilers.Apple;
using ProjectTools.Platforms;


public class XcodeToolchain : AClangToolchain
{
    public const string IPhoneOSVersionMin = "17.0";
    public const string MacOSVersionMin = "13.4";

    private readonly Dictionary<ETargetPlatform, Dictionary<ETargetArch, AppleCompiler>> _appleCompilers;
    private readonly MetalCompiler _metalCompiler = new();

    public readonly string DeveloperPath;
    public readonly string MacOSSdkPath;
    public readonly string IPhoneOSSdkPath;
    public readonly string IPhoneSimulatorSdkPath;
    public readonly string SdkVersion;

    public XcodeToolchain()
    {
        ProcessResult XcodeSelectResult = this.Run(["xcode-select", "-p"]);
        ProcessResult ShowSdkPathResult = this.Run(["xcrun", "--sdk", "macosx", "--show-sdk-path"]);
        ProcessResult IPhoneSdkPathResult = this.Run(["xcrun", "--sdk", "iphoneos", "--show-sdk-path"]);
        ProcessResult IPhoneSimulatorSdkPathResult = this.Run(["xcrun", "--sdk", "iphonesimulator", "--show-sdk-path"]);
        ProcessResult ShowSdkVersionResult = this.Run(["xcrun", "--show-sdk-version"]);

        DeveloperPath = XcodeSelectResult.StandardOutput;
        MacOSSdkPath = ShowSdkPathResult.StandardOutput.TrimEnd();
        IPhoneOSSdkPath = IPhoneSdkPathResult.StandardOutput.TrimEnd();
        IPhoneSimulatorSdkPath = IPhoneSimulatorSdkPathResult.StandardOutput.TrimEnd();
        SdkVersion = ShowSdkVersionResult.StandardOutput;

        if (string.IsNullOrEmpty(DeveloperPath) || string.IsNullOrEmpty(MacOSSdkPath) || string.IsNullOrEmpty(SdkVersion))
        {
            throw new XcodeNotInstalledException();
        }

        Dictionary<ETargetArch, AppleCompiler> IOSPlatform = new()
        {
            { ETargetArch.Arm64, new($"-miphoneos-version-min={IPhoneOSVersionMin}", IPhoneOSSdkPath) },
            { ETargetArch.x64, new($"-miphonesimulator-version-min={IPhoneOSVersionMin}", IPhoneSimulatorSdkPath) },
        };

        AppleCompiler MacOSCompiler = new($"-mmacosx-version-min={MacOSVersionMin}", MacOSSdkPath);
        Dictionary<ETargetArch, AppleCompiler> MacOSPlatform = new()
        {
            { ETargetArch.Arm64, MacOSCompiler },
            { ETargetArch.x64, new($"-mmacosx-version-min={MacOSVersionMin}", MacOSSdkPath) },
        };

        _appleCompilers = new()
        {
            { ETargetPlatform.iOS, IOSPlatform },
            { ETargetPlatform.macOS, MacOSPlatform }
        };
    }

    public override string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo)
    {
        ICompiler Compiler = GetCompiler(InCompileCommandInfo.TargetFile, InCompileCommandInfo.TargetPlatform, InCompileCommandInfo.TargetArch);

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
            _ => ".o"
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

    private ICompiler GetCompiler(FileReference InFile, ETargetPlatform InTargetPlatform, ETargetArch InTargetArch)
    {
        return InFile.Extension switch
        {
            ".metal" => _metalCompiler,
            _ => _appleCompilers[InTargetPlatform][InTargetArch]
        };
    }

    private ICompiler GetCompiler(LinkCommandInfo InInfo)
    {
        return InInfo.Module.BinaryType switch
        {
            EModuleBinaryType.ShaderLibrary => _metalCompiler,
            _ => _appleCompilers[InInfo.TargetPlatform][InInfo.Arch]
        };
    }
}

public class XcodeNotInstalledException : ABaseException;
public class SourceFileExtensionNotSupportedException(string InMessage) : ABaseException(InMessage);
