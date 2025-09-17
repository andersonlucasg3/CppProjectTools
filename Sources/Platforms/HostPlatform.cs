using System.Runtime.InteropServices;

namespace ProjectTools.Platforms;

public abstract class AHostPlatform : ADesktopPlatform
{
    public abstract IReadOnlyDictionary<ETargetPlatform, ATargetPlatform> SupportedTargetPlatforms { get; }

    public static AHostPlatform GetHost()
    {
        if (IsMacOS())
        {
            return new MacPlatform();
        }

        if (IsWindows())
        {
            return new WindowsPlatform();
        }

        throw new PlatformNotSupportedException();
    }

    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}