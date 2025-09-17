using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.Solutions.Sections;

public enum ESectionType
{
    PreProject,
    PostProject,
    PreSolution,
    PostSolution
}

public interface ISection
{
    public ESectionType SectionType { get; }

    public void Build(IndentedStringBuilder InStringBuilder);

    protected void PutContent(IndentedStringBuilder InStringBuilder);
}

public abstract class TSection<T> : ISection
{
    public abstract ESectionType SectionType { get; }

    public abstract void PutContent(IndentedStringBuilder InStringBuilder);

    protected virtual string GetSectionTypeName()
    {
        return typeof(T).Name;
    }

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"{GetSectionName(SectionType)}({GetSectionTypeName()}) = {ToString(SectionType)}");
        InStringBuilder.Indent(() =>
        {
            PutContent(InStringBuilder);
        });
        InStringBuilder.AppendLine($"End{GetSectionName(SectionType)}");
    }

    private static string GetSectionName(ESectionType InType)
    {
        return InType switch
        {
            ESectionType.PreProject => "ProjectSection",
            ESectionType.PostProject => "ProjectSection",
            ESectionType.PreSolution => "GlobalSection",
            ESectionType.PostSolution => "GlobalSection",
            _ => throw new ArgumentOutOfRangeException(nameof(InType), InType, null),
        };
    }

    private static string ToString(ESectionType InType)
    {
        return InType switch
        {
            ESectionType.PreProject => "preProject",
            ESectionType.PostProject => "postProject",
            ESectionType.PreSolution => "preSolution",
            ESectionType.PostSolution => "postSolution",
            _ => throw new ArgumentOutOfRangeException(nameof(InType), InType, null)
        };
    }
}