using Shared.Misc;

namespace Shared.Projects.VisualStudio.Solutions.Sections;

public class NestedProjects(Dictionary<SolutionProject, SolutionProject> InNestedProjectsMap) : TSection<NestedProjects>
{
    public override ESectionType SectionType => ESectionType.PreSolution;

    public readonly Dictionary<SolutionProject, SolutionProject> ParentChildMap = InNestedProjectsMap;

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        foreach (KeyValuePair<SolutionProject, SolutionProject> KVPair in ParentChildMap)
        {
            SolutionProject Child = KVPair.Key;
            SolutionProject Parent = KVPair.Value;

            InStringBuilder.AppendLine($"{Child.ProjectGuid} = {Parent.ProjectGuid}");
        }
    }
}
