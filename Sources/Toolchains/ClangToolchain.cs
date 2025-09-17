namespace ProjectTools.Toolchains;

using IO;
using Projects;
using Processes;
using Exceptions;
using ProjectTools.Platforms;

public abstract class AClangToolchain : IToolchain
{
    public abstract string GetBinaryTypeExtension(EModuleBinaryType BinaryType);
    public abstract string GetBinaryTypePrefix(EModuleBinaryType BinaryType);
    public abstract string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo);
    public abstract string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo);
    public abstract string GetObjectFileExtension(FileReference InSourceFile);
    public abstract string[] GetAutomaticModuleCompilerDefinitions(AModuleDefinition InModule, ETargetPlatform InTargetPlatform);

    public virtual ProcessResult Compile(CompileCommandInfo InCompileCommandInfo)
    {
        return this.Run(GetCompileCommandline(InCompileCommandInfo));
    }

    public virtual ProcessResult Link(LinkCommandInfo InLinkCommandInfo)
    {
        return this.Run(GetLinkCommandLine(InLinkCommandInfo));
    }
}

public class CompilerAlreadyRegisteredException(string InExtension, string InCompilerName) 
    : ABaseException($"Compiler {InCompilerName} already added for extension {InExtension}");