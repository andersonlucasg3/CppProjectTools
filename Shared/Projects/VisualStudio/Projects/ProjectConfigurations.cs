using Shared.Compilation;
using Shared.Platforms;

namespace Shared.Projects.VisualStudio.Projecs;

using ProjectXml;

public class ProjectConfigurations(ECompileConfiguration[] InCompileConfigurations, ETargetPlatform[] InTargetPlatforms) : TItemGroup<ProjectConfiguration>
{
    protected override string TagName => "ItemGroup";

    protected override Parameter[] Parameters => [
        new Parameter("Label", "ProjectConfigurations")    
    ];

    protected override ProjectConfiguration[] Contents => [
        .. InCompileConfigurations.SelectMany(Config => InTargetPlatforms.Select(Plat => new ProjectConfiguration(Config, Plat)))
    ];
}

public class ProjectConfiguration(ECompileConfiguration InCompileConfiguration, ETargetPlatform InTargetPlatform) : TTagGroup<ATag>
{
    protected override Parameter[] Parameters => [
        new Parameter("Include", $"{InCompileConfiguration}|{InTargetPlatform}")
    ];

    protected override ATag[] Contents => [
        new Configuration(InCompileConfiguration),
        new Platform(InTargetPlatform),
    ];
}

public class Configuration(ECompileConfiguration InConfiguration) : ATag(InConfiguration.ToString());
public class Platform(ETargetPlatform InPlatform) : ATag(InPlatform.ToString());