namespace ProjectTools.Projects.VisualStudio.ProjectXml;

public class PreprocessorDefinitions(string InPreprocessorDefinition) : ATag($"{InPreprocessorDefinition};%({nameof(PreprocessorDefinitions)})");