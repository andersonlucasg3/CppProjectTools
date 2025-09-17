namespace ProjectTools.Projects.VisualStudio.Projecs;

using ProjectXml;

public class Import(string InProject) : ATag
{
    protected override Parameter[] Parameters { get; } = [
        new Parameter("Project", InProject),
    ];
}
