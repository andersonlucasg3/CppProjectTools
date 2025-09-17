using ProjectTools.Compilation;
using ProjectTools.Extensions;
using ProjectTools.Misc;
using ProjectTools.Platforms;

namespace ProjectTools.Projects.VisualStudio.Solutions.Sections;

public enum EProjectConfigurationType
{
    ActiveConfig,
    Build,
}

public class ProjectConfigurationPlatform(SolutionProject InProjects, ECompileConfiguration InCompileConfiguration, ETargetPlatform InTargetPlatform, EProjectConfigurationType InProjectConfigurationType, bool bUseSolutionPlatform)
{
    private readonly bool _bUseSolutionPlatform = bUseSolutionPlatform;

    public readonly SolutionGuid ProjectGuid = InProjects.ProjectGuid;
    public readonly ECompileConfiguration CompileConfiguration = InCompileConfiguration;
    public readonly ETargetPlatform TargetPlatform = InTargetPlatform;
    public readonly EProjectConfigurationType ProjectConfigurationType = InProjectConfigurationType;

    public void Build(IndentedStringBuilder InStringBuilder)
    {
        string TargetPlatformString = _bUseSolutionPlatform ? TargetPlatform.ToSolutionPlatform() : TargetPlatform.ToString();
        InStringBuilder.AppendLine($"{ProjectGuid}.{CompileConfiguration}|{TargetPlatformString}.{ProjectConfigurationType} = {CompileConfiguration}|{TargetPlatformString}");
    }
}

public class ProjectConfigurationPlatforms : TSection<ProjectConfigurationPlatforms>
{
    public override ESectionType SectionType => ESectionType.PostSolution;

    public readonly ProjectConfigurationPlatform[] ConfigurationPlatforms;

    public ProjectConfigurationPlatforms(SolutionProject[] InProjects, EProjectConfigurationType[] InProjectConfigurationTypes)
    {
        List<ProjectConfigurationPlatform> ConfigurationPlatformsList = [];

        foreach (SolutionProject Project in InProjects)
        {
            foreach (ECompileConfiguration CompileConfiguration in Project.CompileConfigurations)
            {
                foreach (ETargetPlatform TargetPlatform in Project.TargetPlatforms)
                {
                    foreach (EProjectConfigurationType ProjectConfigurationType in InProjectConfigurationTypes)
                    {
                        ConfigurationPlatformsList.Add(new(Project, CompileConfiguration, TargetPlatform, ProjectConfigurationType, TargetPlatform == ETargetPlatform.Any));
                    }
                }
            }
        }

        ConfigurationPlatforms = [.. ConfigurationPlatformsList];
    }

    public override void PutContent(IndentedStringBuilder InStringBuilder)
    {
        foreach (ProjectConfigurationPlatform ConfigurationPlatform in ConfigurationPlatforms)
        {
            ConfigurationPlatform.Build(InStringBuilder);
        }
    }
}
