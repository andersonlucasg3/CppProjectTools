namespace ProjectTools.Sources;

using Platforms;

public class AppleSourceCollection(ETargetPlatform InTargetPLatform, string[] InExcludedSourcePaths) : CppSourceCollection(InTargetPLatform, InExcludedSourcePaths)
{
    public static readonly string[] ObjCSourceFilesExtensions = [".m", ".mi"];
    public static readonly string[] ObjCppSourceFilesExtensions = [".mm", ".mii"];

    protected override string[] GetSourceFilesExtensions()
    {
        return [
            .. base.GetSourceFilesExtensions(),
            .. ObjCSourceFilesExtensions,
            .. ObjCppSourceFilesExtensions,
        ];
    }
}