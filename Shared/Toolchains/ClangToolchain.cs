namespace Shared.Toolchains;

using IO;
using Sources;
using Projects;
using Processes;
using Exceptions;
using Shared.Platforms;

public abstract class ClangToolchain : IToolchain
{
    public abstract string GetBinaryTypeExtension(EModuleBinaryType BinaryType);
    public abstract string GetBinaryTypePrefix(EModuleBinaryType BinaryType);
    public abstract string[] GetCompileCommandline(CompileCommandInfo InCompileCommandInfo);
    public abstract string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo);
    public abstract string GetObjectFileExtension(FileReference InSourceFile);

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
    : BaseException($"Compiler {InCompilerName} already added for extension {InExtension}");