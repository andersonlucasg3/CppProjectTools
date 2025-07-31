using System.Text;

namespace Shared.Misc;

public class IndentedStringBuilder
{
    private readonly StringBuilder _builder = new();
    private uint _indentLevel = 0;

    public void Indent(Action IndentedAction)
    {
        _indentLevel += 1;
        IndentedAction.Invoke();
        _indentLevel -= 1;
    }

    public void Append(string InContent, bool bIndent = true)
    {
        string Indentation = bIndent ? new('\t', (int)_indentLevel) : "";

        _builder.Append($"{Indentation}{InContent}");
    }

    public void AppendLine(string InContent)
    {
        string Indentation = new('\t', (int)_indentLevel);

        _builder.AppendLine($"{Indentation}{InContent}");
    }

    public void Clear()
    {
        _indentLevel = 0;
        _builder.Clear();
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}

public interface IIndentedStringBuildable
{
    public void Build(IndentedStringBuilder InStringBuilder);
}