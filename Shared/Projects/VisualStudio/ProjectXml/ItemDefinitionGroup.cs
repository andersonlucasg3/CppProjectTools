using Shared.IO;
using Shared.Compilation;
using Shared.Platforms;

namespace Shared.Projects.VisualStudio.ProjectXml;

public class ItemDefinitionGroup(string[] InPreprocessorDefinitions, DirectoryReference[] InIncludeDirectories, ECompileConfiguration InCompileConfiguration, ETargetPlatform InTargetPlatform) : TTagGroup<ItemDefinitionGroup.ClCompile>
{
    protected override Parameter[] Parameters => [ 
		new Parameter("Condition", $"'$(Configuration)|$(Platform)'=='{InCompileConfiguration}|{InTargetPlatform}'"),
    ];

    protected override ClCompile[] Contents => [
		new ClCompile(InPreprocessorDefinitions, InIncludeDirectories)
	];

    public class ClCompile(string[] InPreprocessorDefinitions, DirectoryReference[] InIncludeDirectories) : TTagGroup<ATag>
    {
        protected override ATag[] Contents => [
            .. InPreprocessorDefinitions.Select(Definition => new PreprocessorDefinitions(Definition)),
            .. InIncludeDirectories.Select(IncludeDirectory => new AdditionalIncludeDirectories(IncludeDirectory)),
            new LanguageStandard(), new LanguageStandard_C(),
        ];
    }
}
