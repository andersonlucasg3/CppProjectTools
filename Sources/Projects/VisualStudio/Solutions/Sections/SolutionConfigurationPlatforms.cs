using ProjectTools.Compilation;
using ProjectTools.Extensions;
using ProjectTools.Misc;
using ProjectTools.Platforms;

namespace ProjectTools.Projects.VisualStudio.Solutions.Sections;

public class SolutionConfigurationPlatform(ECompileConfiguration InConfiguration, ETargetPlatform InPlatform, bool bUseSolutionPlatform) 
    : IIndentedStringBuildable, IEquatable<SolutionConfigurationPlatform>
{
    private readonly bool _bUseSolutionPlatform = bUseSolutionPlatform;

    public readonly ECompileConfiguration CompileConfiguration = InConfiguration;
    public readonly ETargetPlatform TargetPlatform = InPlatform;

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        string TargetPlatformString = _bUseSolutionPlatform ? TargetPlatform.ToSolutionPlatform() : TargetPlatform.ToString();
        InStringBuilder.AppendLine($"{CompileConfiguration}|{TargetPlatformString} = {CompileConfiguration}|{TargetPlatformString}");
    }

    public bool Equals(SolutionConfigurationPlatform? Other)
    {
        if (Other is null) return true;

        return CompileConfiguration == Other?.CompileConfiguration &&
               TargetPlatform == Other?.TargetPlatform;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as SolutionConfigurationPlatform);
    }

    public override int GetHashCode()
    {
        return CompileConfiguration.GetHashCode() + TargetPlatform.GetHashCode();
    }
}

public class SolutionConfigurationPlatforms : TSection<SolutionConfigurationPlatforms>
{
    public override ESectionType SectionType => ESectionType.PreSolution;

    public readonly SolutionConfigurationPlatform[] ConfigurationPlatforms;

    public SolutionConfigurationPlatforms(SolutionProject[] InProjects)
    {
        HashSet<SolutionConfigurationPlatform> ConfigurationPlatformsList = [];
        foreach (SolutionProject Project in InProjects.Distinct())
        {
            foreach (ECompileConfiguration CompileConfiguration in Project.CompileConfigurations)
            {
                foreach (ETargetPlatform Platform in Project.TargetPlatforms)
                {
                    SolutionConfigurationPlatform SCP = new(CompileConfiguration, Platform, Platform == ETargetPlatform.Any);

                    ConfigurationPlatformsList.Add(SCP);
                }
            }
        }
        ConfigurationPlatforms = [.. ConfigurationPlatformsList];
    }

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        foreach (SolutionConfigurationPlatform ConfigurationPlatform in ConfigurationPlatforms)
        {
            ConfigurationPlatform.Build(InStringBuilder);
        }
    }
}

