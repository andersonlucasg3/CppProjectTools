using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.Solutions.Sections;

public class ExtensibilityGlobals : TSection<ExtensibilityGlobals>
{
    public override ESectionType SectionType => ESectionType.PostSolution;

    public SolutionGuid SolutionGuid = SolutionGuid.NewGuid();

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"""
        SolutionGuid = {SolutionGuid}
        """);
    }
}
