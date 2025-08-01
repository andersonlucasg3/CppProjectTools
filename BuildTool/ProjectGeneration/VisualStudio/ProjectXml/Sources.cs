﻿using Shared.IO;
using Shared.Sources;

namespace BuildTool.ProjectGeneration.VisualStudio.ProjectXml;

public class Sources(ISourceCollection InSourceCollection) : TItemGroup<Sources.ClCompile>
{
    protected override Parameter[] Parameters => [];

    protected override ClCompile[] Contents => [.. InSourceCollection.SourceFiles.Select(SourceFile => new ClCompile(SourceFile))];

    public class ClCompile(FileReference InFile) : Tag
    {
        protected override Parameter[] Parameters => [
            new Parameter("Include", $"$(SolutionDir){InFile.RelativePath}"),
        ];
    }
}



public class Headers(ISourceCollection InSourceCollection) : TItemGroup<Headers.ClInclude>
{
    protected override ClInclude[] Contents => [
        .. InSourceCollection.HeaderFiles.Select(HeaderFile => new ClInclude(HeaderFile))
    ];

    public class ClInclude(FileReference InFile) : Tag
    {
        protected override Parameter[] Parameters => [
            new Parameter("Include", $"$(SolutionDir){InFile.RelativePath}"),
        ];
    }
}
