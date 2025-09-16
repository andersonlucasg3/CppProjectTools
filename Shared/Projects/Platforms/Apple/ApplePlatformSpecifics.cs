using Shared.IO;

namespace Shared.Projects.Platforms.Apple;

public class ApplePlatformSpecifics
{
    private readonly List<DirectoryReference> _frameworkSearchPaths = [];

    protected virtual HashSet<string> _frameworkDependencies { get; } = ["CoreFoundation", "Foundation"];

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