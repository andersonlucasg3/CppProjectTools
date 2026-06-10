using System.Diagnostics;
using System.Text;

namespace ProjectTools.Processes;

public class ProcessResult
{
    public int ExitCode;
    
    public string StandardOutput = "";
    public string StandardError = "";
    
    public bool bSuccess => ExitCode == 0;
}

public static class ProcessExecutorExtension
{
    public static ProcessResult Run(this object _, string[] InCommandLine, bool bPrintCommandLine = false)
    {
        return Run(InCommandLine, bPrintCommandLine);
    }

    public static ProcessResult Run(string[] InCommandLine, bool bPrintCommandLine = false)
    {
        Process RunProcess = new();
        
        string Executable = InCommandLine[0];
        IEnumerable<string> Arguments = InCommandLine.Skip(1);

        if (bPrintCommandLine)
        {
            Console.WriteLine($"INFO:  {string.Join(' ', InCommandLine)}");
        }

        RunProcess.StartInfo = new()
        {
            FileName = Executable,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        // Use ArgumentList for proper escaping of paths with spaces
        foreach (string Arg in Arguments)
        {
            RunProcess.StartInfo.ArgumentList.Add(Arg);
        }

        ProcessResult Result = new();
        RunProcess.OutputDataReceived += (_, Args) =>
        {
            if (Args.Data is not null)
            {
                string NewOutputData = $"{Args.Data}{Environment.NewLine}";
                lock (Result) Result.StandardOutput += NewOutputData;
            }
        };
        RunProcess.ErrorDataReceived += (_, Args) =>
        {
            if (Args.Data is not null)
            {
                string NewErrorData = $"{Args.Data}{Environment.NewLine}";
                lock (Result) Result.StandardError += NewErrorData;
            }
        };

        if (RunProcess.Start())
        {
            RunProcess.BeginOutputReadLine();
            RunProcess.BeginErrorReadLine();
            RunProcess.WaitForExit();
            // Wait for async read operations to drain before reading exit code
            RunProcess.WaitForExit();
        }

        Result.ExitCode = RunProcess.ExitCode;

        return Result;
    }
}

