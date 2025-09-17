using ProjectTools.Sources;

namespace ProjectTools.Projects.VisualStudio.Filters;

using ProjectXml;

public class Filter(string InName, ISourceCollection InSourceCollection) : TTagGroup<ATag>
{
    protected override string TagName => "Filter";
    
    protected override Parameter[] Parameters => [
        new Parameter("Include", InName),
    ];

    protected override ATag[] Contents => [
        new UniqueIdentifier(),
        new Extensions(string.Join(';', InSourceCollection.AllFilesExtensions)),
    ];
}

public class Filters(string[] InFilters, ISourceCollection InSourceCollection) : TItemGroup<Filter>
{
    protected override Parameter[] Parameters { get; } = [];
    protected override Filter[] Contents => [
        .. InFilters.Select(InFilter => new Filter(InFilter, InSourceCollection))
    ];
}