using Shared.Extensions;
using Shared.Misc;
using Shared.Platforms;

namespace Shared.Projects.VisualStudio.Solutions.Sections;

public class GuidSection(ETargetPlatform[] InTargetPlatforms) : TSection<GuidSection>
{
    public override ESectionType SectionType => ESectionType.PreSolution;

    protected override string GetSectionTypeName()
    {
        return "ddbf523f-7eb6-4887-bd51-85a714ff87eb";
    }

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.Append($"AvailablePlatforms=");
        for (int Index = 0; Index < InTargetPlatforms.Length; Index++)
        {
            ETargetPlatform TargetPlatform = InTargetPlatforms[Index];
            
            if (Index > 0)
            {
                InStringBuilder.Append($";", false);
            }

            if (TargetPlatform == ETargetPlatform.Any)
            {
                InStringBuilder.Append($"{TargetPlatform.ToSolutionPlatform()}", false);
                continue;
            }

            InStringBuilder.Append($"{TargetPlatform}", false);
        }
        InStringBuilder.Append(Environment.NewLine, false);
    }
}
