using ProjectTools.Compilation;
using ProjectTools.Misc;
using ProjectTools.Platforms;

namespace ProjectTools.Projects.VisualStudio.Solutions;

public enum ESolutionProjectKind
{
    CSharpProject,
    CppProject,
    Folder
}

public class SolutionProject(
    string InProjectName, 
    string InProjectPath, 
    ECompileConfiguration[]? InCompileConfigurations,
    ETargetPlatform[]? InTargetPlatforms,
    ESolutionProjectKind InProjectKind = ESolutionProjectKind.CppProject)
{
    public readonly SolutionGuid ProjectKindGuid = InProjectKind switch
    {
        ESolutionProjectKind.CppProject => SolutionGuid.Parse("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"),
        ESolutionProjectKind.CSharpProject => SolutionGuid.Parse("FAE04EC0-301F-11D3-BF8B-00C04F79EFBC"),
        ESolutionProjectKind.Folder => SolutionGuid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8"),
        _ => throw new ArgumentOutOfRangeException(nameof(InProjectKind), InProjectKind, null)
    };

    public readonly string ProjectName = InProjectName;
    public readonly string ProjectPath = InProjectPath;
    public readonly ESolutionProjectKind ProjectKind = InProjectKind;

    public readonly ECompileConfiguration[] CompileConfigurations = InCompileConfigurations ?? [];
    public readonly ETargetPlatform[] TargetPlatforms = InTargetPlatforms ?? [];

    public readonly SolutionGuid ProjectGuid = SolutionGuid.NewGuid();

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"""
        Project("{ProjectKindGuid}") = "{ProjectName}", "{ProjectPath}", "{ProjectGuid}"
        EndProject
        """);
    }
}

