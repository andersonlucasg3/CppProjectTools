namespace Shared.Toolchains.Compilers;

using Sources;

public abstract class ACppCompiler : ICompiler
{
    public const string CppStandard = "c++23";

    public virtual string[] CCompiledSourceExtensions { get; } = [.. CppSourceCollection.CSourceFilesExtensions];
    public virtual string[] CppCompiledSourceExtensions { get; } = [.. CppSourceCollection.CppSourceFileExtensions];

    public abstract string[] GetCompileCommandLine(CompileCommandInfo InCompileCommandInfo);
    public abstract string[] GetLinkCommandLine(LinkCommandInfo InLinkCommandInfo);
}