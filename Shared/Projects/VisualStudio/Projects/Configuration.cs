using Shared.Platforms;
using Shared.Compilation;

namespace Shared.Projects.VisualStudio.Projecs;

using ProjectXml;

public class ConfigurationPropertyGroup(EModuleBinaryType InBinaryType, ECompileConfiguration InCompileConfiguration, ETargetPlatform InTargetPlatform) : APropertyGroup
{
    public readonly bool bUseDebugLibraries = InCompileConfiguration == ECompileConfiguration.Debug;
    public readonly string PlatformToolset = "v143";
    public readonly string CharacterSet = "Unicode";
    public readonly bool bWholeProgramOptimization = InCompileConfiguration == ECompileConfiguration.Release;

    protected override Parameter[] Parameters => [
        new Parameter("Label", "Configuration"),
        new Parameter("Condition", $"'$(Configuration)|$(Platform)'=='{InCompileConfiguration}|{InTargetPlatform}'"),
    ];

    protected override ATag[] Contents => [
        new ConfigurationType(InBinaryType),
        new UseDebugLibraries(bUseDebugLibraries),
        new PlatformToolset(PlatformToolset),
        new CharacterSet(CharacterSet),
        new WholeProgramOptimization(bWholeProgramOptimization),
    ];
}

public class ConfigurationType(EModuleBinaryType InBinaryType) : ATag(InBinaryType.ToString());
public class UseDebugLibraries(bool bInUseDebugLibraries) : ATag(bInUseDebugLibraries.ToString());
public class PlatformToolset(string InPlatformToolset) : ATag(InPlatformToolset);
public class CharacterSet(string InCharacterSet) : ATag(InCharacterSet);
public class WholeProgramOptimization(bool bInWholeProgramOptimization) : ATag(bInWholeProgramOptimization.ToString());
