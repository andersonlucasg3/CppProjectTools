using Shared.Projects;
using Shared.Toolchains;

namespace Shared.Platforms;

public class VisionOSPlatform(XcodeToolchain InToolchain) : AMobilePlatform
{
    public override ETargetPlatform Platform => ETargetPlatform.visionOS;
    public override IToolchain Toolchain => InToolchain;

    public override AModuleDefinition GetSingleModuleInstance()
    {
        throw new NotImplementedException();
    }
}