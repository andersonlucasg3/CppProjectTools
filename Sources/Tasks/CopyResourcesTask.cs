using ProjectTools.IO;
using ProjectTools.Projects;

namespace ProjectTools.Tasks;

public class CopyResourcesTask(AModuleDefinition InModule)
{
    public void Copy()
    {
        if (InModule.CopyResourcesDirectories.Count == 0) return; // if nothing to do, just do nothing

        Console.WriteLine($"Copying resources for module: {InModule.Name}");

        DirectoryReference BinariesDirectory = ProjectDirectories.Shared.CreateBaseConfigurationDirectory(ECompileBaseDirectory.Binaries);

        foreach (DirectoryReference Directory in InModule.CopyResourcesDirectories)
        {
            FileReference[] SourceAllFiles = Directory.EnumerateFiles("*", SearchOption.AllDirectories);

            DirectoryReference DestinationDirectory = BinariesDirectory.Combine(Directory.Name);
            FileReference[] DestinationAllFiles = DestinationDirectory.EnumerateFiles("*", SearchOption.AllDirectories);

            List<CopyAction> CopyActions = [];
            List<FileReference> FilesToRemove = [.. DestinationAllFiles];

            foreach (FileReference SourceFile in SourceAllFiles)
            {
                FileReference DestinationFile = SourceFile.FullPath.Replace(Directory.FullPath, DestinationDirectory.FullPath);

                FileInfo SourceFileInfo = SourceFile.GetInfo();
                FileInfo DestinationFileInfo = DestinationFile.GetInfo();

                if (!DestinationFile.bExists || SourceFileInfo.Length != DestinationFileInfo.Length)
                {
                    CopyActions.Add(new(SourceFile, DestinationFile));
                }

                FilesToRemove.Remove(DestinationFile);
            }

            foreach (FileReference FileToRemove in FilesToRemove)
            {
                FileToRemove.Delete();
            }

            foreach (CopyAction CopyAction in CopyActions)
            {
                CopyAction.Execute();
            }
        }
    }

    private readonly struct CopyAction(FileReference InSourceFile, FileReference InDestinationFile)
    {
        public readonly FileReference SourceFile = InSourceFile;
        public readonly FileReference DestinationFile = InDestinationFile;

        public readonly void Execute()
        {
            SourceFile.CopyTo(DestinationFile, true, true);
        }
    }
}