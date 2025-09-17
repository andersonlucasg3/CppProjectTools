using ProjectTools.Exceptions;

namespace ProjectTools.CommandLines;

public class CommandLine
{
    public CommandArgument Command { get; }
    public IReadOnlyDictionary<string, ICommandLineArgument> Arguments { get; }
    
    public CommandLine(string[] InArgs)
    {
        CommandArgument? FoundCommandArgument = null;
        Dictionary<string, ICommandLineArgument> NewArguments = [];
        foreach (string ArgumentString in InArgs)
        {
            ICommandLineArgument Argument = ICommandLineArgument.Parse(ArgumentString);

            if (Argument is CommandArgument CommandArgument)
            {
                FoundCommandArgument = CommandArgument;
                
                continue;
            }

            NewArguments.Add(Argument.Name, Argument);
        }
        Arguments = NewArguments;

        Command = FoundCommandArgument ?? throw new MissingCommandException(string.Join(" ", InArgs));
    }

    public void PrintCommandLine()
    {
        Console.Write($"Executing: {Command}");
        
        foreach (ICommandLineArgument Arg in Arguments.Values)
        {
            Console.Write($" {Arg.ToString()}");
        }
        
        Console.WriteLine();
    }
}

public class MissingCommandException(string? InMessage) : ABaseException(InMessage);