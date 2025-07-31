using Shared.IO;

namespace Shared.Projects.VisualStudio.Filters;

using ProjectXml;

public class Sources(Dictionary<FileReference, string> InSourcesFilterMap) : TItemGroup<ClCompile>
{
    protected override Parameter[] Parameters => [];

    protected override ClCompile[] Contents => [
        .. InSourcesFilterMap.Keys.Select(SourceFile => new ClCompile(SourceFile, InSourcesFilterMap[SourceFile]))
    ];
}

public class ClCompile(FileReference InFile, string InFilter) : TTagGroup<SourceFilter>
{
    protected override Parameter[] Parameters => [
        new Parameter("Include", $"$(SolutionDir){InFile.RelativePath}"),
    ];

    protected override SourceFilter[] Contents => [new SourceFilter(InFilter)];
}

public class Headers(Dictionary<FileReference, string> InHeadersFilterMap) : TItemGroup<ClInclude>
{
    protected override ClInclude[] Contents => [
        .. InHeadersFilterMap.Keys.Select(HeaderFile => new ClInclude(HeaderFile, InHeadersFilterMap[HeaderFile]))
    ];
}

public class ClInclude(FileReference InFile, string InFilter) : TTagGroup<SourceFilter>
{
    protected override Parameter[] Parameters => [
        new Parameter("Include", $"$(SolutionDir){InFile.RelativePath}"),
    ];

    protected override SourceFilter[] Contents => [new SourceFilter(InFilter)];
}

public class SourceFilter(string InFilter) : ATag(InFilter)
{
    protected override string TagName => "Filter";
}