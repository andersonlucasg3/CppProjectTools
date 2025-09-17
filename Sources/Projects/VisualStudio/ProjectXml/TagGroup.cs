using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.ProjectXml;

public abstract class TTagGroup<T> : IIndentedStringBuildable
    where T : IIndentedStringBuildable
{
    protected virtual string TagName { get; }
    protected virtual Parameter[] Parameters { get; } = [];

    protected abstract T[] Contents { get; }

    public TTagGroup()
    {
        TagName = GetType().Name;
    }

    public virtual void Build(IndentedStringBuilder InStringBuilder)
    {
        string ParametersString = Parameters.Length > 0 ? $" {string.Join(' ', (IEnumerable<Parameter>)Parameters)}" : "";

        InStringBuilder.AppendLine($"<{TagName}{ParametersString}>");
        InStringBuilder.Indent(() =>
        {
            foreach (T Element in Contents)
            {
                Element.Build(InStringBuilder);
            }
        });
        InStringBuilder.AppendLine($"</{TagName}>");
    }
}
