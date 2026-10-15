namespace ProjectTools.Sources;

using Platforms;

public class WindowsSourceCollection(ETargetPlatform InTargetPlatform, string[] InExcludedSourcePaths) : CppSourceCollection(InTargetPlatform, InExcludedSourcePaths);