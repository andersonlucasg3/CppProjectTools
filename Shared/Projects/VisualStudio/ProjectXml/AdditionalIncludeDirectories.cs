using Shared.IO;

namespace Shared.Projects.VisualStudio.ProjectXml;

public class AdditionalIncludeDirectories(DirectoryReference InIncludeDirectory) : ATag($"{InIncludeDirectory.PlatformPath};%({nameof(AdditionalIncludeDirectories)})");