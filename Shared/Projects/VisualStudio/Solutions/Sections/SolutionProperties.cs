using Shared.Misc;

namespace Shared.Projects.VisualStudio.Solutions.Sections;

public class SolutionProperties : TSection<SolutionProperties>
{
    public override ESectionType SectionType => ESectionType.PreSolution;

    public required SolutionBool bHideSolutionNode;

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"""
        HideSolutionNode = {bHideSolutionNode}
        """);
    }
}
