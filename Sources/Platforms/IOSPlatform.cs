using ProjectTools.Projects;
using ProjectTools.Toolchains;

namespace ProjectTools.Platforms;

public class IOSPlatform(XcodeToolchain InToolchain) : AMobilePlatform
{
    public override ETargetPlatform Platform => ETargetPlatform.iOS;

    public override IToolchain Toolchain { get; } = InToolchain;

    public override AModuleDefinition GetSingleModuleInstance()
    {
        throw new NotImplementedException();
    }
}