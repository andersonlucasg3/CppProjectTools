using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.Solutions;

using Sections;

public class SolutionGlobal(SolutionProject[] InProjects, Dictionary<SolutionProject, SolutionProject> InNestedProjectsMap)
{
    public readonly ISection[] GlobalSections = [
        new SolutionConfigurationPlatforms(InProjects),
        new GuidSection([.. InProjects.SelectMany(Project => Project.TargetPlatforms).Distinct()]),
        new ProjectConfigurationPlatforms(InProjects, Enum.GetValues<EProjectConfigurationType>()),
        new SolutionProperties() { bHideSolutionNode = false },
        new NestedProjects(InNestedProjectsMap),
        new ExtensibilityGlobals(),
    ];

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"""
        Global
        """);
        InStringBuilder.Indent(() =>
        {
            foreach (ISection GlobalSection in GlobalSections)
            {
                GlobalSection.Build(InStringBuilder);
            }
        });
        InStringBuilder.AppendLine($"""
        EndGlobal
        """);
    }
}

