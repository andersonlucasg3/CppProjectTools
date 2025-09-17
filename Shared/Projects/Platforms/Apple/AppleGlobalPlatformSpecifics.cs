using Shared.IO;

namespace Shared.Projects.Platforms.Apple;

public class AppleGlobalPlatformSpecifics(ApplePlatformSpecifics[] InAllPlatforms)
{
    public AppleGlobalPlatformSpecifics AddFrameworkDependencies(params string[] InFrameworkNames)
    {
        foreach (ApplePlatformSpecifics Platform in InAllPlatforms)
        {
            Platform.AddFrameworkDependencies(InFrameworkNames);
        }

        return this;
    }

    public AppleGlobalPlatformSpecifics AddFrameworkSearchPaths(params DirectoryReference[] InSearchPaths)
    {
        foreach (ApplePlatformSpecifics Platform in InAllPlatforms)
        {
            Platform.AddFrameworkSearchPaths(InSearchPaths);
        }

        return this;
    }
}