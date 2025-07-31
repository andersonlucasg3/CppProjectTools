namespace Shared.Projects.VisualStudio.ProjectXml;

public class Target(string InCommandName, string? InCommand = null, Parameter[]? InExtraParameters = null) : TTagGroup<ATag>
{
    protected override Parameter[] Parameters => [
        new Parameter("Name", InCommandName),
        .. InExtraParameters ?? [],
    ];

    protected override ATag[] Contents
    {
        get
        {
            List<ATag> TagList = [new Message($"{InCommandName} $(ProjectName) $(Platform) $(Configuration)")];
            if (!string.IsNullOrEmpty(InCommand))
            {
                TagList.Add(new Exec(InCommand));
            }
            return [.. TagList];
        }
    }
}

public class Message(string InText) : ATag
{
    protected override Parameter[] Parameters => [
        new Parameter("Text", InText),
        new Parameter("Importance", "high"),
    ];
}

public class Exec(string InCommand) : ATag
{
    protected override Parameter[] Parameters => [
        new Parameter("Command", InCommand),
        new Parameter("WorkingDirectory", "$(SolutionDir)"),
    ];
}
