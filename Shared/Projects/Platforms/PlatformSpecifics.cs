using Shared.Platforms;

namespace Shared.Projects.Platforms;

using Apple;

public class PlatformSpecifics
{
    private readonly Dictionary<ETargetPlatform, ApplePlatformSpecifics> _appleSpecificsMap = new()
    {
        { ETargetPlatform.macOS, new MacPlatformSpecifics() },
        { ETargetPlatform.iOS, new IOSPlatformSpecifics() },
    };

    public MacPlatformSpecifics GetMac() => (MacPlatformSpecifics)_appleSpecificsMap[ETargetPlatform.macOS];
    public IOSPlatformSpecifics GetIOS() => (IOSPlatformSpecifics)_appleSpecificsMap[ETargetPlatform.iOS];

    internal ApplePlatformSpecifics Get(ETargetPlatform InTargetPlatform) => _appleSpecificsMap[InTargetPlatform];
}