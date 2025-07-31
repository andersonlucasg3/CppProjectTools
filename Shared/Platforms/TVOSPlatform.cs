using Shared.Toolchains;

namespace Shared.Platforms;

public class TVOSPlatform(XcodeToolchain InToolchain) : IOSPlatform(InToolchain)
{
    public override ETargetPlatform Platform => ETargetPlatform.tvOS;
}