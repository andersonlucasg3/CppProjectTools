namespace Shared.Platforms;

using Projects;
using Toolchains;

public class AndroidPlatform(AndroidToolchain InToolchain) : AMobilePlatform
{
    public override ETargetPlatform Platform => ETargetPlatform.Android;

    public override IToolchain Toolchain => InToolchain;

    public override AModuleDefinition GetSingleModuleInstance()
    {
        return new AndroidSingleModuleDefiniton();
    }
}

public class AndroidSingleModuleDefiniton : AModuleDefinition
{
    public override EModuleBinaryType BinaryType => EModuleBinaryType.DynamicLibrary;

    public override string Name => "AndroidNative";

    protected override string SourcesDirectoryName => ".";

    protected override void Configure(ATargetPlatform InTargetPlatform)
    {
               
    }
}