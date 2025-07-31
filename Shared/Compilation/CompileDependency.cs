using System.Text.RegularExpressions;

namespace Shared.Compilation;

using IO;
using Sources;

public partial class CompileDependency
{  
    public readonly FileReference? ObjectFile = null;
    public readonly FileReference? SourceFile = null;
    public readonly FileReference[] DependencyHeaderFiles = [];

    public readonly bool bValidDependency;
    
    public CompileDependency(FileReference InDependencyFile, string InObjectFileExtension, ISourceCollection InSourceCollection)
    {
        bValidDependency = false;
        
        if (!InDependencyFile.bExists) return;
        
        string DependencyFileContents = InDependencyFile.ReadAllText();
        Regex Regex = GetDependencyFileRegex();
        MatchCollection MatchCollection = Regex.Matches(DependencyFileContents);

        List<FileReference> DependencyHeadersList = [];
        foreach (Match Match in MatchCollection)
        {
            string MatchValue = Match.Value.Trim();
            
            string Extension = Path.GetExtension(MatchValue);
            
            if (Extension == InObjectFileExtension)
            {
                ObjectFile = MatchValue;
                continue;
            }

            if (InSourceCollection.SourceFilesExtensions.Contains(Extension))
            {
                SourceFile = MatchValue;
                continue;
            }

            if (!InSourceCollection.HeaderFilesExtensions.Contains(Extension)) continue;
            
            DependencyHeadersList.Add(MatchValue);
        }

        DependencyHeaderFiles = [.. DependencyHeadersList];

        bValidDependency = this is { ObjectFile: not null, SourceFile: not null };
    }

    public CompileDependency(FileReference InSourceFile, FileReference InObjectFile, FileReference[] InDependencyHeaderFiles)
    {
        SourceFile = InSourceFile;
        ObjectFile = InObjectFile;
        DependencyHeaderFiles = InDependencyHeaderFiles;

        bValidDependency = this is { ObjectFile: not null, SourceFile: not null };
    }

    public void WriteToFile(FileReference InFile)
    {
        string Contents = $"""
        {ObjectFile?.FullPath}: \
        {SourceFile?.FullPath} \
        """;

        for (int Index = 0; Index < DependencyHeaderFiles.Length; Index++)
        {
            FileReference Header = DependencyHeaderFiles[Index];
            Contents = $"{Contents}{Environment.NewLine}{Header.FullPath}";
            if (Index < DependencyHeaderFiles.Length - 1)
            {
                Contents += " \\";
            }
        }

        InFile.WriteAllText(Contents);
    }

    [GeneratedRegex("[A-Z:/a-zA-Z0-9-_\\\\s\\\\(\\\\).]+[^:][^:\\s\\\\]", RegexOptions.Multiline)]
    private static partial Regex GetDependencyFileRegex();
}

