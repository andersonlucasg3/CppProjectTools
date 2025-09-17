using ProjectTools.Platforms;

namespace ProjectTools.Projects;

using IO;
using Compilation;
using ProjectTools.Extensions;


public enum ECompileBaseDirectory
{
    Binaries,
    Intermediate
}

public class ProjectDirectories
{
    private static ProjectDirectories? _shared;
    public static ProjectDirectories Shared => _shared!;

    private readonly AProjectDefinition _inProjectDefinition;
    private readonly ATargetPlatform _inTargetPlatform;
    private readonly ECompileConfiguration _inConfiguration;
    
    private ProjectDirectories(AProjectDefinition InProjectDefinition, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration)
    {
        _inProjectDefinition = InProjectDefinition;
        _inTargetPlatform = InTargetPlatform;
        _inConfiguration = InConfiguration;
    }
    
    public static void Create(AProjectDefinition InProjectDefinition, ATargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration) 
        => _shared = new(InProjectDefinition, InTargetPlatform, InConfiguration);

    public DirectoryReference CreateIntermediateProjectsDirectory(bool bCreate = true)
    {
        DirectoryReference IntermediateDirectory = CreateBaseDirectory(ECompileBaseDirectory.Intermediate, bCreate);

        DirectoryReference ProjectsDirectory = IntermediateDirectory.Combine("Projects");
        if (bCreate && !ProjectsDirectory.bExists) ProjectsDirectory.Create();

        return ProjectsDirectory;
    }

    public DirectoryReference CreateBaseConfigurationDirectory(ECompileBaseDirectory InBaseDirectory, bool bCreate = true)
    {
        DirectoryReference BaseDirectory = CreateBaseDirectory(InBaseDirectory, bCreate);

        DirectoryReference PlatformDirectory = BaseDirectory.Combine(_inTargetPlatform.Name);
        if (bCreate && !PlatformDirectory.bExists) PlatformDirectory.Create();

        DirectoryReference ConfigurationDirectory = PlatformDirectory.Combine(_inConfiguration.ToString());
        if (bCreate && !ConfigurationDirectory.bExists) ConfigurationDirectory.Create();

        return ConfigurationDirectory;
    }

    public static DirectoryReference CreateBaseDirectory(ECompileBaseDirectory InBaseDirectory, bool bCreate = true)
    {
        string Name = InBaseDirectory.ToString();

        DirectoryReference RootDirectory = Environment.CurrentDirectory;
        DirectoryReference IntermediateDirectory = RootDirectory.Combine(Name);
        if (bCreate && !IntermediateDirectory.bExists) IntermediateDirectory.Create();

        return IntermediateDirectory;
    }

    public DirectoryReference CreateModuleDirectory(ECompileBaseDirectory BaseDirectory, string ModuleName, bool bCreate = true)
    {
        DirectoryReference ConfigurationDirectory = CreateBaseConfigurationDirectory(BaseDirectory, bCreate);

        DirectoryReference FinalModuleDirectory = ConfigurationDirectory.Combine(ModuleName);
        if (bCreate && !FinalModuleDirectory.bExists) FinalModuleDirectory.Create();

        return FinalModuleDirectory;
    }

    public DirectoryReference CreateModuleSubDirectory(ECompileBaseDirectory BaseDirectory, string ModuleName, string SubDirectoryName, bool bCreate = true)
    {
        DirectoryReference ModuleDirectory = CreateModuleDirectory(BaseDirectory, ModuleName, bCreate);
        
        DirectoryReference ObjectsDirectory = ModuleDirectory.Combine(SubDirectoryName);
        if (bCreate && !ObjectsDirectory.bExists) ObjectsDirectory.Create();
        
        return ObjectsDirectory;
    }

    public DirectoryReference CreateIntermediateObjectsDirectory(string ModuleName, bool bCreate = true)
    {
        return CreateModuleSubDirectory(ECompileBaseDirectory.Intermediate, ModuleName, "Objects", bCreate);
    }

    public DirectoryReference CreateIntermediateChecksumsDirectory(ETargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration, bool bCreate = true)
    {
        DirectoryReference IntermediateDirectory = CreateBaseDirectory(ECompileBaseDirectory.Intermediate, bCreate);

        DirectoryReference ChecksumsDirectory = IntermediateDirectory.Combine("Checksums", InTargetPlatform.ToSourcePlatformName(), InConfiguration.ToString());
        if (!ChecksumsDirectory.bExists) ChecksumsDirectory.Create();

        return ChecksumsDirectory;
    }
}