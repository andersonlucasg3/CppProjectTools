namespace Shared.Platforms;

public abstract class ADesktopPlatform : ATargetPlatform
{
    public override bool bSupportsModularLinkage { get; } = true;
}