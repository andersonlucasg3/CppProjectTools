using ProjectTools.IO;
using ProjectTools.Misc;
using ProjectTools.Projects.VisualStudio.ProjectXml;

namespace ProjectTools.Projects.VisualStudio.CharpProjects;

public class CSharpProject(DirectoryReference InProjectRoot, FileReference[] InSourceFiles) : TTagGroup<IIndentedStringBuildable>
{
    protected override string TagName => "Project";

    protected override Parameter[] Parameters { get; } = [new Parameter("Sdk", "Microsoft.NET.Sdk")];

    protected override IIndentedStringBuildable[] Contents { get; } = [
        new PropertyGroup(),
        new ItemGroup(InProjectRoot, InSourceFiles),
    ];

    class ItemGroup(DirectoryReference InProjectRoot, FileReference[] InSourceFiles) : APropertyGroup
    {
        protected override string TagName => "ItemGroup";

        protected override ATag[] Contents => [
            .. InSourceFiles.Select(Each => new Compile(Each)),
            new ProjectReference(InProjectRoot),
        ];
    }

    class PropertyGroup : APropertyGroup
    {
        protected override ATag[] Contents => [
            new CustomTag("OutputType", "library"),
            new CustomTag("TargetFramework", "net9.0"),
            new CustomTag("ImplicitUsings", "enable"),
            new CustomTag("Nullable", "enable"),
        ];
    }

    class Compile(FileReference InSourceFile) : ATag
    {
        protected override Parameter[] Parameters => [ new Parameter("Include", InSourceFile.PlatformPath) ];
    }

    class ProjectReference(DirectoryReference InProjectRoot) : ATag
    {
        protected override Parameter[] Parameters => [
            new Parameter("Include", FindProjectToolsCSProj(InProjectRoot))
        ];

        private static string FindProjectToolsCSProj(DirectoryReference InProjectRoot)
        {
            DirectoryReference[] DirectoryReferences = InProjectRoot.EnumerateDirectories("ProjectTools", SearchOption.AllDirectories);
            if (DirectoryReferences.Length < 1)
            {
                throw new ProjectNotFoundException($"Could not find ProjectTools folder in any of the subdirectories from root: {InProjectRoot.PlatformPath}");
            }

            FileReference[] FileReferences = DirectoryReferences[0].EnumerateFiles("ProjectTools.csproj", SearchOption.AllDirectories);
            if (FileReferences.Length < 1)
            {
                throw new ProjectNotFoundException($"Could not find ProjectTools.csproj in any of the subdirectories from root: {InProjectRoot.PlatformPath}");
            }

            return FileReferences[0].PlatformPath;
        }
    }

    class CustomTag(string InName, string InValue) : ATag(InValue)
    {
        protected override string TagName => InName;
    }
}