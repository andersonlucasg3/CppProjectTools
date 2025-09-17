using ProjectTools.IO;

namespace ProjectTools.Projects.Platforms.Apple;

public class ApplePlatformSpecifics
{
    private readonly HashSet<string> _frameworkDependencies = ["CoreFoundation", "Foundation"];
    private readonly List<DirectoryReference> _frameworkSearchPaths = [];

    public IReadOnlySet<string> FrameworkDependencies => _frameworkDependencies;
    public IReadOnlyList<DirectoryReference> FrameworkSearchPaths => _frameworkSearchPaths;

    public ApplePlatformSpecifics AddFrameworkDependencies(params string[] InFrameworkNames)
    {
        foreach (string FrameworkName in InFrameworkNames)
        {
            _frameworkDependencies.Add(FrameworkName);
        }

        return this;
    }

    public ApplePlatformSpecifics AddFrameworkSearchPaths(params DirectoryReference[] InSearchPaths)
    {
        _frameworkSearchPaths.AddRange(InSearchPaths);

        return this;
    }
}