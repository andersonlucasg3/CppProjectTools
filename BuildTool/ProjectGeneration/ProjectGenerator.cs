using Shared.Exceptions;
using Shared.Projects;

namespace BuildTool.ProjectGeneration;

public enum EProjectGeneratorType
{
    Clang,
    VisualStudio
}

public interface IProjectGenerator
{
    public void Generate();
}

public class ProjectGeneratorNotSupportedException(string InMessage) : BaseException(InMessage);