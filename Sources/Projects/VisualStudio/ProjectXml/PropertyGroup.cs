namespace ProjectTools.Projects.VisualStudio.ProjectXml;

public abstract class APropertyGroup : TTagGroup<ATag>
{
    protected override string TagName => "PropertyGroup";
}
