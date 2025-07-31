
namespace Shared.Projects.VisualStudio.ProjectXml;

using Solutions;

public class Project(SolutionGuid InGuid) : ATag(InGuid.ToString());