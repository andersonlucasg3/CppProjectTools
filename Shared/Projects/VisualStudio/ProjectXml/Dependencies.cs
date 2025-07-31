namespace Shared.Projects.VisualStudio.ProjectXml;

using Solutions;

public class Dependencies(SolutionProject[] InProjects) : TItemGroup<ProjectReference>
{
    protected override Parameter[] Parameters => [];

    protected override ProjectReference[] Contents => [
        .. InProjects.Select(Project => new ProjectReference(Project))
    ];
}

public class ProjectReference(SolutionProject InProject) : TItemGroup<Project>
{
    protected override string TagName => "ProjectReference";

    protected override Parameter[] Parameters => [
        new Parameter("Include", $"$(SolutionDir){InProject.ProjectPath}"),
    ];

    protected override Project[] Contents => [
        new Project(InProject.ProjectGuid)
    ];
}