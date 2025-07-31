using Shared.CommandLines;
using Shared.Exceptions;
using Shared.Reflections;

namespace Shared.Commands;

public interface IExecutableCommand
{
    public string Name { get; }
    public string Example { get; }

    public bool Execute(IReadOnlyDictionary<string, ICommandLineArgument> Arguments);

    public static IExecutableCommand[] GetAllCommands()
    {
        Type[] Commands = TypeFinder.FindChildClasses<IExecutableCommand>();

        return Commands.Select(Type => Activator.CreateInstance(Type) as IExecutableCommand).ToArray()!;
    }
    
    public static IExecutableCommand? GetCommand(CommandArgument InCommandArgument)
    {
        IExecutableCommand[] Commands = GetAllCommands();

        return Commands.FirstOrDefault(Command => Command!.Name.Equals(InCommandArgument.Name), null);
    }
}

public static class ExecutableCommandExtensions
{
    public static TValue? GetArgumentValue<TValue>(this IReadOnlyDictionary<string, ICommandLineArgument> InArguments, string InName, bool bIsRequired = false)
    {
        if (!InArguments.TryGetValue(InName, out ICommandLineArgument? Argument))
        {
            if (bIsRequired) throw new MissingRequiredArgumentException(InName);

            return default;
        }

        if (Argument.TryGetValue(out TValue? Value)) return Value;
        
        if (bIsRequired) throw new MissingRequiredArgumentException(Argument.OriginalArgument);

        return default;
    }

    public static TValue[] GetArrayArgument<TValue>(this IReadOnlyDictionary<string, ICommandLineArgument> InArguments, string InName, bool bIsRequired = false)
    {
        TValue? Value = InArguments.GetArgumentValue<TValue>(InName, bIsRequired) ?? default;
        TValue?[] Values = InArguments.GetArgumentValue<TValue[]>(InName, bIsRequired) ?? new TValue?[] { Value };
        TValue[] NonNullValues = [.. Values.Where(EachValue => EachValue is not null)!];
        return NonNullValues;
    }
}

public class MissingRequiredArgumentException(string InMessage) : ABaseException(InMessage);