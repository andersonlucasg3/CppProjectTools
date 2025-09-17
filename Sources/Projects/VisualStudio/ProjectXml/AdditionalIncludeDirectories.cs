using ProjectTools.IO;

namespace ProjectTools.Projects.VisualStudio.ProjectXml;

public class AdditionalIncludeDirectories(DirectoryReference InIncludeDirectory) : ATag($"{InIncludeDirectory.PlatformPath};%({nameof(AdditionalIncludeDirectories)})");