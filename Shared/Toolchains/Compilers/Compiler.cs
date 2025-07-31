namespace Shared.Toolchains.Compilers;

public interface ICompiler
{
    public string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo);
    public string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo);

    public string GetObjectFileExtension();
}