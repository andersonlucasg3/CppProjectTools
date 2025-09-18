using ProjectTools.Platforms;

namespace ProjectTools.Compilation.Actions;

public interface IAdditionalCompileAction
{
    bool Execute(CompileModuleInfo InModuleInfo, ATargetPlatform InTargetPlatform);
}