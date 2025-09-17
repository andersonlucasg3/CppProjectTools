using ProjectTools.Misc;

namespace ProjectTools.Projects.VisualStudio.Solutions;

public class SolutionHeader()
{
    public readonly string FormatVersion = "12.00";
    public readonly uint VisualStudioVersionInt = 17;
    public readonly string VisualStudioVersion = "17.13.35931.197";
    public readonly string MinimumVisualStudioVersion = "10.0.40219.1";

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        InStringBuilder.AppendLine($"""
        Microsoft Visual Studio Solution File, Format Version {FormatVersion}
        # Visual Studio Version {VisualStudioVersionInt}
        VisualStudioVersion = {VisualStudioVersion}
        MinimumVisualStudioVersion = {MinimumVisualStudioVersion}
        """);
    }
}

