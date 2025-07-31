using Shared.IO;
using Shared.Projects;
using Shared.Commands;
using Shared.Processes;
using Shared.CommandLines;
using BuildTool.Compilation.Commands;

IExecutableCommand? ExecutingCommand = null;
CommandLine? CommandLine = null;

DateTime StartedTime = DateTime.Now;

try
{
    DirectoryReference.Initialize();
    FileReference.Initialize();

    CommandLine = new(args);
    CommandLine.PrintCommandLine();

    bool bDisableMultiThreading = CommandLine.Arguments.ContainsKey("DisableMT");

    if (bDisableMultiThreading)
    {
        ThreadWorker.SingleThreaded();
    }
    else
    {
        int ProcessCount = CommandLine.Arguments.GetArgumentValue<int>("ProcessCount");
        ProcessCount = ProcessCount == 0 ? Environment.ProcessorCount : ProcessCount;

        ThreadWorker.PreallocateThreads(ProcessCount);

        Console.WriteLine($"Running with {ProcessCount} threads");
    }

    ExecutingCommand = IExecutableCommand.GetCommand(CommandLine.Command);
    if (ExecutingCommand is not null)
    {
        bool bResult = ExecutingCommand.Execute(CommandLine.Arguments);

        if (!bResult)
        {
            Environment.ExitCode = -1;
        }
    }
}
catch (MissingRequiredArgumentException Ex)
{
    Console.WriteLine($"Missing required argument: {Ex.Message}. Usage example:");
    Console.WriteLine($"    {ExecutingCommand?.Name} {ExecutingCommand?.Example}");

    Environment.ExitCode = -1;
}
catch (MissingCommandException)
{
    IExecutableCommand[] Commands = IExecutableCommand.GetAllCommands();

    Console.WriteLine("Available commands:");
    foreach (IExecutableCommand Command in Commands)
    {
        Console.WriteLine($"    {Command.Name} {Command.Example}");
    }

    Environment.ExitCode = -1;
}
catch (ProjectNotFoundException Ex)
{
    Console.WriteLine($"Project not found: {Ex.Message}");

    Environment.ExitCode = -1;
}
catch (TargetPlatformNotSupportedException Ex)
{
    Console.WriteLine(Ex.Message);

    Environment.ExitCode = -1;
}

if (CommandLine is null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Could not parse command line:");
    Console.WriteLine(string.Join(' ', args));
}

if (ExecutingCommand is null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Could not find any command named {CommandLine?.Command.Name}");
}

ThreadWorker.Terminate();

Console.ResetColor();
Console.WriteLine($"BuildTool took {(DateTime.Now - StartedTime).TotalSeconds:F2} seconds to execute.");