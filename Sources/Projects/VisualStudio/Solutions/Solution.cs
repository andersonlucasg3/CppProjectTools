using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.Solutions;

public class Solution(SolutionProject[] InProjects, Dictionary<SolutionProject, SolutionProject> InNestedProjectsMap)
{
    public readonly SolutionHeader Header = new();
    public readonly SolutionProject[] Projects = InProjects;
    public readonly SolutionGlobal Global = new(InProjects, InNestedProjectsMap);

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        Header.Build(InStringBuilder);
        
        foreach (SolutionProject Project in Projects)
        {
            Project.Build(InStringBuilder);
        }

        Global.Build(InStringBuilder);
    }
}

