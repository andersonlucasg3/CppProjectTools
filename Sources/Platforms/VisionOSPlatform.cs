using ProjectTools.Projects;
using ProjectTools.Toolchains;

namespace ProjectTools.Platforms;

public class VisionOSPlatform(XcodeToolchain InToolchain) : AMobilePlatform
{
    public override ETargetPlatform Platform => ETargetPlatform.visionOS;
    public override IToolchain Toolchain => InToolchain;

    public override AModuleDefinition GetSingleModuleInstance()
    {
        throw new NotImplementedException();
    }
}