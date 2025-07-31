namespace Shared.Platforms;

using Projects;

public abstract class AMobilePlatform : ATargetPlatform
{
    public override bool bSupportsModularLinkage { get; } = false;

    public abstract AModuleDefinition GetSingleModuleInstance();
}