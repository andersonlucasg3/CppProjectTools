using Shared.Platforms;

namespace Shared.Extensions;

public static class TargetPlatformExtensions
{
    public static string ToSolutionPlatform(this ETargetPlatform InTargetPlatform)
    {
        return InTargetPlatform switch
        {
            ETargetPlatform.Any => "Any CPU",
            ETargetPlatform.iOS => "iOS",
            ETargetPlatform.tvOS => "tvOS",
            ETargetPlatform.visionOS => "visionOS",
            ETargetPlatform.macOS => "macOS",
            ETargetPlatform.Android => "android-arm64-v8",
            ETargetPlatform.Windows => "x64",
            _ => throw new Platforms.PlatformNotSupportedException(InTargetPlatform),
        };
    }

    public static string ToSourcePlatformName(this ETargetPlatform InTargetPlatform)
    {
        return InTargetPlatform switch
        {
            ETargetPlatform.iOS => "IOS",
            ETargetPlatform.tvOS => "TVOS",
            ETargetPlatform.visionOS => "VisionOS",
            ETargetPlatform.macOS => "Mac",
            ETargetPlatform.Android => "Android",
            ETargetPlatform.Windows => "Windows",
            _ => throw new Platforms.PlatformNotSupportedException(InTargetPlatform),
        };
    }

    public static ETargetPlatformGroup GetPlatformGroup(this ETargetPlatform InTargetPlatform)
    {
        return InTargetPlatform switch
        {
            ETargetPlatform.Any => ETargetPlatformGroup.Any,
            ETargetPlatform.iOS => ETargetPlatformGroup.Apple,
            ETargetPlatform.tvOS => ETargetPlatformGroup.Apple,
            ETargetPlatform.visionOS => ETargetPlatformGroup.Apple,
            ETargetPlatform.macOS => ETargetPlatformGroup.Apple,
            ETargetPlatform.Android => ETargetPlatformGroup.Google,
            ETargetPlatform.Windows => ETargetPlatformGroup.Microsoft,
            _ => throw new Platforms.PlatformNotSupportedException(InTargetPlatform),
        };
    }

    public static ETargetPlatformType GetPlatformType(this ETargetPlatform InTargetPlatform)
    {
        return InTargetPlatform switch
        {
            ETargetPlatform.Any => ETargetPlatformType.Any,
            ETargetPlatform.iOS => ETargetPlatformType.Mobile,
            ETargetPlatform.tvOS => ETargetPlatformType.Mobile,
            ETargetPlatform.visionOS => ETargetPlatformType.Mobile,
            ETargetPlatform.macOS => ETargetPlatformType.Desktop,
            ETargetPlatform.Android => ETargetPlatformType.Mobile,
            ETargetPlatform.Windows => ETargetPlatformType.Desktop,
            _ => throw new Platforms.PlatformNotSupportedException(InTargetPlatform),
        };
    }

    public static ETargetPlatform[] GetTargetPlatformsInGroup(this ETargetPlatformGroup InGroup)
    {
        return InGroup switch
        {
            ETargetPlatformGroup.Any => [
                ETargetPlatform.Any,
            ],
            ETargetPlatformGroup.Apple => [
                ETargetPlatform.iOS,
                ETargetPlatform.tvOS,
                ETargetPlatform.visionOS,
                ETargetPlatform.macOS,
            ],
            ETargetPlatformGroup.Google => [
                ETargetPlatform.Android,
            ],
            ETargetPlatformGroup.Microsoft => [
                ETargetPlatform.Windows,
            ],
            _ => throw new PlatformGroupNotSupportedException(InGroup),
        };
    }
}
