using Shared.Misc;

namespace Shared.Projects.VisualStudio.ProjectXml;

public abstract class TItemGroup<T> : TTagGroup<T>
    where T : IIndentedStringBuildable
{
    protected override string TagName => "ItemGroup";
}
