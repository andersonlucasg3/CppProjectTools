using Shared.Toolchains;

namespace Shared.Platforms;

public class MacPlatform : AHostPlatform
{
    public override IReadOnlyDictionary<ETargetPlatform, ATargetPlatform> SupportedTargetPlatforms { get; }

    public override ETargetPlatform Platform => ETargetPlatform.macOS;
    public override IToolchain Toolchain { get; }

    public MacPlatform()
    {
        XcodeToolchain XcodeToolchain = new();
        AndroidToolchain AndroidToolchain = new();
        
        SupportedTargetPlatforms = new Dictionary<ETargetPlatform, ATargetPlatform>
        {
            { ETargetPlatform.macOS, this },
            { ETargetPlatform.iOS, new IOSPlatform(XcodeToolchain) },
            { ETargetPlatform.tvOS, new TVOSPlatform(XcodeToolchain) },
            { ETargetPlatform.visionOS, new VisionOSPlatform(XcodeToolchain) },
            { ETargetPlatform.Android, new AndroidPlatform(AndroidToolchain) }
        };
        
        Toolchain = XcodeToolchain;
    }
}

