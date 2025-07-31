using Shared.Compilation;
using Shared.Projects;
using Shared.Sources;

namespace BuildTool.Compilation;

public enum ECompilationResult
{
    Waiting,
    NothingToCompile,
    CompilationSuccess,
    CompilationFailed,
}

public enum ELinkageResult
{
    Waiting,
    LinkUpToDate,
    LinkSuccess,
    LinkFailed,
}

public class CompileModuleInfo(AModuleDefinition InModule, ISourceCollection InSourceCollection, LinkAction InLinkAction)
{
    public readonly AModuleDefinition Module = InModule;
    public readonly ISourceCollection SourceCollection = InSourceCollection;
    public readonly LinkAction Link = InLinkAction;

    public string ModuleName => Module.Name;

    public ECompilationResult CompileResult = ECompilationResult.Waiting;
    public ELinkageResult LinkResult = ELinkageResult.Waiting;
    public CompileAction[] CompileActions = [];
}