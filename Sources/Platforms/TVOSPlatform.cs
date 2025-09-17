using ProjectTools.Toolchains;

namespace ProjectTools.Platforms;

public class TVOSPlatform(XcodeToolchain InToolchain) : IOSPlatform(InToolchain)
{
    public override ETargetPlatform Platform => ETargetPlatform.tvOS;
}