using Shared.Toolchains;

namespace Shared.Platforms;

public class WindowsPlatform : AHostPlatform
{
    public override IReadOnlyDictionary<ETargetPlatform, ATargetPlatform> SupportedTargetPlatforms { get; }
    
    public override ETargetPlatform Platform => ETargetPlatform.Windows;
    public override IToolchain Toolchain { get; }

    public WindowsPlatform()
    {
        Toolchain = new VisualStudioToolchain();

        AndroidToolchain AndroidToolchain = new();

        SupportedTargetPlatforms = new Dictionary<ETargetPlatform, ATargetPlatform>
        {
            { ETargetPlatform.Windows, this },
            { ETargetPlatform.Android, new AndroidPlatform(AndroidToolchain) },
        };
    }
}
