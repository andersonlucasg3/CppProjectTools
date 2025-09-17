namespace ProjectTools.Toolchains;

using IO;
using Projects;
using Platforms;
using Exceptions;
using Compilers.Android;

public class AndroidToolchain : AClangToolchain
{
    const string SupportedAndroidNdkVersion = "29.0.13846066";
    // const int AndroidNdkApiVersion = 35;
    const int MinimumSupportedAndroidNdkVersion = 21;
    const string CompilingAndroidArch = "aarch64"; // TODO: this should be configurable

    private readonly AndroidCompiler _compiler;

    public AndroidToolchain()
    {
        string AndroidPrebuiltPlatform;
        string ExpectedAndroidSdkPath;
        if (AHostPlatform.IsWindows())
        {
            AndroidPrebuiltPlatform = "windows-x86_64";
            ExpectedAndroidSdkPath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE")!, "AppData", "Local", "Android", "Sdk");
        }
        else if (AHostPlatform.IsMacOS())
        {
            AndroidPrebuiltPlatform = "darwin-x86_64";
            ExpectedAndroidSdkPath = Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "Library", "Android", "sdk");
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        string ExpectedAndroidNdkPath = Path.Combine(ExpectedAndroidSdkPath, "ndk", SupportedAndroidNdkVersion);


        DirectoryReference ExpectedAndroidSdkDirectory = ExpectedAndroidSdkPath;
        DirectoryReference AndroidHome = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? ExpectedAndroidSdkPath;

        DirectoryReference AndroidNdk = ExpectedAndroidNdkPath;

        if (!AndroidHome.bExists)
        {
            AndroidHome = ExpectedAndroidSdkDirectory;
        }

        if (!AndroidHome.bExists || !AndroidNdk.bExists)
        {
            throw new AndroidSdkNotInstalledException(AndroidHome.bExists, AndroidNdk.bExists);
        }

        DirectoryReference PrebuiltPlatformRoot = AndroidNdk.Combine("toolchains", "llvm", "prebuilt", AndroidPrebuiltPlatform);

        _compiler = new(PrebuiltPlatformRoot, CompilingAndroidArch, MinimumSupportedAndroidNdkVersion);
    }

    public override string GetBinaryTypeExtension(EModuleBinaryType BinaryType)
    {
        return BinaryType switch
        {
            EModuleBinaryType.Application => "", // Android only supports shared libraries as targets
            EModuleBinaryType.StaticLibrary => ".a",
            EModuleBinaryType.DynamicLibrary => ".so",
            _ => throw new ArgumentOutOfRangeException(nameof(BinaryType), BinaryType, null)
        };
    }

    public override string GetBinaryTypePrefix(EModuleBinaryType InBinaryType)
    {
        return InBinaryType switch
        {
            EModuleBinaryType.Application => "",
            EModuleBinaryType.StaticLibrary => "lib",
            EModuleBinaryType.DynamicLibrary => "lib",
            EModuleBinaryType.ShaderLibrary => throw new ShaderLibraryNotSupportedOnPlatformException(InBinaryType),
            _ => throw new ArgumentOutOfRangeException(nameof(InBinaryType), InBinaryType, null)
        };
    }

    public override string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo)
    {
        return _compiler.GetCompileCommandLine(InCompileCommandInfo);
    }

    public override string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo)
    {
        return _compiler.GetLinkCommandLine(InLinkCommandInfo);
    }

    public override string GetObjectFileExtension(FileReference InSourceFile)
    {
        return ".o";
    }

    public override string[] GetAutomaticModuleCompilerDefinitions(AModuleDefinition InModule, ETargetPlatform InTargetPlatform)
    {
        List<string> CompilerDefinitions = [
            $"ANDROID_PLATFORM=android-{MinimumSupportedAndroidNdkVersion}",
        ];

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
}

public class AndroidSdkNotInstalledException(bool bHaveSdk, bool bHaveNdk) : ABaseException($"HaveSdk: {bHaveSdk}, HaveNdk: {bHaveNdk}");