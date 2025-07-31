using Shared.Projects;
using Shared.Toolchains;

namespace Shared.Platforms;

public class IOSPlatform(XcodeToolchain InToolchain) : AMobilePlatform
{
    public override ETargetPlatform Platform => ETargetPlatform.iOS;

    public override IToolchain Toolchain { get; } = InToolchain;

    public override AModuleDefinition GetSingleModuleInstance()
    {
        throw new NotImplementedException();
    }
}