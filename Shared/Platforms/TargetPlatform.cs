using Shared.Exceptions;
using Shared.Toolchains;

namespace Shared.Platforms;

public enum ETargetPlatform
{
    Any,
    iOS,
    tvOS,
    visionOS,
    macOS,
    Android,
    Windows,
}

public enum ETargetPlatformGroup
{
    Any,
    Apple,
    Google,
    Microsoft,
}

public enum ETargetPlatformType
{
    Any,
    Mobile,
    Desktop,
}

public abstract class ATargetPlatform
{
    public virtual string Name => Platform.ToString();
    public abstract ETargetPlatform Platform { get; }
    public abstract IToolchain Toolchain { get; }
    public abstract bool bSupportsModularLinkage { get; }
}

public class PlatformNotSupportedException : ABaseException
{
    public PlatformNotSupportedException() : base() { }
    public PlatformNotSupportedException(ETargetPlatform InTargetPlatform) : base($"{InTargetPlatform}") { }
}

public class PlatformGroupNotSupportedException : ABaseException
{
    public PlatformGroupNotSupportedException() : base() { }
    public PlatformGroupNotSupportedException(ETargetPlatformGroup InTargetPlatformGroup) : base($"{InTargetPlatformGroup}") { }
}