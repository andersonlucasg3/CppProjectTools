using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.IO;
using Shared.Platforms;
using Shared.Projects;
using Shared.Toolchains;

namespace Shared.Compilation;

public class ChecksumData
{
    [JsonInclude]
    public string? FileChecksum = null;

    [JsonInclude]
    public string? CommandLineChecksum = null;

    [JsonInclude]
    public bool bCompileSucceeded = false;

    [JsonInclude]
    public Dictionary<string, string> DependencyHeadersChecksumMap = [];

    public bool ChecksumsMatch(string InFileChecksum, string InCommandLineChecksum)
    {
        return FileChecksum == InFileChecksum && CommandLineChecksum == InCommandLineChecksum;
    }
}

public class ChecksumStorage
{
    public static readonly ChecksumStorage Shared = new();

    private readonly Lock _lock = new();
    private readonly SHA256 _sha = SHA256.Create();
    private readonly JsonSerializerOptions _sharedOptions = new() { WriteIndented = true };

    private Dictionary<string, ChecksumData> _checksumDataMap = [];

    // used to avoid generating header file checksums when already generated 
    private readonly Dictionary<string, string> _memoryFileChecksumMap = [];
    private readonly Dictionary<string, string> _commandLineChecksumMap = [];

    ~ChecksumStorage()
    {
        _sha.Dispose();
    }

    public bool ShouldRecompile(CompileAction InAction, IToolchain InToolchain)
    {
        lock (_lock)
        {
            // if the checksum is not on the map, we need to compile it
            if (!_checksumDataMap.TryGetValue(InAction.SourceFile.RelativePath, out ChecksumData? ChecksumData))
            {
                return true;
            }

            GenerateChecksums(InAction, InToolchain, out string FileChecksum, out string CommandLineChecksum);

            // If the source file or the command-line changed, we don't need to look to the dependencies
            if (!ChecksumData.ChecksumsMatch(FileChecksum, CommandLineChecksum))
            {
                return true;
            }

            // then, we check all dependency headers for changes
            bool bShouldRecompile = false;
            // need the nullable here due to shader libraries not having dependency files
            foreach (FileReference HeaderFile in InAction.Dependency?.DependencyHeaderFiles ?? [])
            {
                if (!HeaderFile.bExists)
                {
                    ChecksumData.DependencyHeadersChecksumMap.Remove(HeaderFile.RelativePath);

                    continue;
                }

                string CurrentHeaderChecksum = GenerateFileChecksum(HeaderFile);

                bool bDontHaveHeaderChecksumData = !ChecksumData.DependencyHeadersChecksumMap.TryGetValue(HeaderFile.RelativePath, out string? HeaderChecksum);
                bool bChecksumMismatch = HeaderChecksum != CurrentHeaderChecksum;

                if (!bShouldRecompile)
                {
                    bShouldRecompile = bDontHaveHeaderChecksumData || bChecksumMismatch;
                }

                ChecksumData.DependencyHeadersChecksumMap[HeaderFile.RelativePath] = CurrentHeaderChecksum;
            }
            return bShouldRecompile;
        }
    }

    public void CompilationSuccess(CompileAction InAction, IToolchain InToolchain)
    {
        lock (_lock)
        {
            string RelativePath = InAction.SourceFile.RelativePath;

            if (!_checksumDataMap.TryGetValue(RelativePath, out ChecksumData? ChecksumData))
            {
                _checksumDataMap.Add(RelativePath, ChecksumData = new());
            }

            GenerateChecksums(InAction, InToolchain, out ChecksumData.FileChecksum, out ChecksumData.CommandLineChecksum);

            ChecksumData.bCompileSucceeded = true;

            foreach (FileReference HeaderFile in InAction.Dependency?.DependencyHeaderFiles ?? [])
            {
                if (!HeaderFile.bExists)
                {
                    ChecksumData.DependencyHeadersChecksumMap.Remove(HeaderFile.RelativePath);

                    continue;
                }

                string CurrentHeaderChecksum = GenerateFileChecksum(HeaderFile);

                ChecksumData.DependencyHeadersChecksumMap[HeaderFile.RelativePath] = CurrentHeaderChecksum;
            }
        }
    }

    public void CompilationFailed(CompileAction InAction)
    {
        lock (_lock)
        {
            if (!_checksumDataMap.TryGetValue(InAction.SourceFile.RelativePath, out ChecksumData? ChecksumData))
            {
                _checksumDataMap.Add(InAction.SourceFile.RelativePath, new());

                return;
            }

            ChecksumData.bCompileSucceeded = false;
        }
    }

    public void LoadChecksums(ETargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration)
    {
        DirectoryReference ChecksumsDirectory = ProjectDirectories.Shared.CreateIntermediateChecksumsDirectory(InTargetPlatform, InConfiguration);

        FileReference ChecksumsFile = ChecksumsDirectory.CombineFile("Cached.checksums");

        if (ChecksumsFile.bExists)
        {
            ChecksumsFile.OpenRead(InFileStream =>
            {
                try
                {
                    _checksumDataMap = JsonSerializer.Deserialize<Dictionary<string, ChecksumData>>(InFileStream) ?? [];
                }
                catch { } // do nothing
            });
        }
    }

    public void SaveChecksums(ETargetPlatform InTargetPlatform, ECompileConfiguration InConfiguration)
    {
        DirectoryReference ChecksumsDirectory = ProjectDirectories.Shared.CreateIntermediateChecksumsDirectory(InTargetPlatform, InConfiguration);

        FileReference ChecksumsFile = ChecksumsDirectory.CombineFile("Cached.checksums");

        if (ChecksumsFile.bExists) ChecksumsFile.Delete();

        ChecksumsFile.OpenWrite(InFileStream => JsonSerializer.Serialize(InFileStream, _checksumDataMap, _sharedOptions));
    }

    private void GenerateChecksums(CompileAction InAction, IToolchain InToolchain, out string OutFileChecksum, out string OutCommandLineChecksum)
    {
        OutFileChecksum = GenerateFileChecksum(InAction.SourceFile);
        OutCommandLineChecksum = GenerateCommandLineChecksum(InToolchain, InAction.CompileCommandInfo);
    }

    private string GenerateFileChecksum(FileReference InFile)
    {
        if (_memoryFileChecksumMap.TryGetValue(InFile.RelativePath, out string? CachedChecksum))
        {
            return CachedChecksum;
        }

        bool bGotIt = false;

        string Checksum = "";
        do
        {
            try
            {
                InFile.OpenRead(FileStream =>
                {
                    Checksum = Convert.ToHexString(_sha.ComputeHash(FileStream));
                    bGotIt = true;
                });
            }
            catch
            {
                Thread.Sleep(1);
            }
        }
        while (!bGotIt);

        _memoryFileChecksumMap.Add(InFile.RelativePath, Checksum);

        return Checksum;
    }

    private string GenerateCommandLineChecksum(IToolchain InToolchain, CompileCommandInfo InCompileCommandInfo)
    {
        string CommandLineString = string.Join(' ', InToolchain.GetCompileCommandline(InCompileCommandInfo));

        if (_commandLineChecksumMap.TryGetValue(InCompileCommandInfo.TargetFile.RelativePath, out string? CommandLineChecksum))
        {
            return CommandLineChecksum;
        }

        bool bGotIt = false;

        do
        {
            try
            {
                byte[] StringBytes = Encoding.UTF8.GetBytes(CommandLineString);
                CommandLineChecksum = Convert.ToHexString(_sha.ComputeHash(StringBytes));
                bGotIt = true;
            }
            catch
            {
                Thread.Sleep(1);
            }
        }
        while (!bGotIt);

        _commandLineChecksumMap.Add(InCompileCommandInfo.TargetFile.RelativePath, CommandLineChecksum!);

        return CommandLineChecksum!;
    }
}