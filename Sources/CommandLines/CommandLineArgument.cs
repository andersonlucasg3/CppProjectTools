using ProjectTools.Exceptions;

namespace ProjectTools.CommandLines;

public interface ICommandLineArgument
{
    public string Name { get; }

    public string OriginalArgument { get; }

    public virtual bool TryGetValue<T>(out T? Value)
    {
        throw new NotImplementedException();
    }

    public virtual string GetStringValue()
    {
        throw new NotImplementedException();
    }

    public virtual string[] GetStringArrayValue()
    {
        throw new NotImplementedException();
    }

    public virtual int GetIntValue()
    {
        throw new NotImplementedException();
    }

    public virtual int[] GetIntArrayValue()
    {
        throw new NotImplementedException();
    }

    public virtual float GetFloatValue()
    {
        throw new NotImplementedException();
    }

    public virtual float[] GetFloatArrayValue()
    {
        throw new NotImplementedException();
    }

    public string? ToString()
    {
        return OriginalArgument;
    }

    public static ICommandLineArgument Parse(string InArgument)
    {
        if (!InArgument.StartsWith('-'))
        {
            if (InArgument.Contains('=')) throw new InvalidCommandException(InArgument);
            return new CommandArgument(InArgument);
        }

        if (!InArgument.StartsWith('-')) throw new UnknownArgumentException(InArgument);

        if (!InArgument.Contains('=')) return new ValuelessArgument(InArgument);
        if (InArgument.Contains(',')) return new MultiValueArgument(InArgument);
        return new SingleValueArgument(InArgument);
    }
}

public sealed class CommandArgument(string InArgument) : ICommandLineArgument
{
    public string OriginalArgument { get; } = InArgument;

    public string Name { get; } = InArgument;

    public override string ToString()
    {
        return OriginalArgument;
    }
}

public sealed class ValuelessArgument(string InArgument) : ICommandLineArgument
{
    public string OriginalArgument { get; } = InArgument;

    public string Name { get; } = InArgument.Replace("-", "");

    public override string ToString()
    {
        return OriginalArgument;
    }
}

public sealed class SingleValueArgument : ICommandLineArgument
{
    public string OriginalArgument { get; }
    public string Name { get; }
    private string Value { get; }

    public SingleValueArgument(string InArgument)
    {
        OriginalArgument = InArgument;
        string[] Components = InArgument.Split("=");
        Name = Components[0].Replace("-", "");
        Value = Components[1];
    }

    public bool TryGetValue<T>(out T? OutValue)
    {
        if (typeof(T) == typeof(string))
        {
            OutValue = (T)(object)GetStringValue();
            return true;
        }

        if (typeof(T) == typeof(int))
        {
            OutValue = (T)(object)GetIntValue();
            return true;
        }

        if (typeof(T) == typeof(float))
        {
            OutValue = (T)(object)GetFloatValue();
            return true;
        }

        OutValue = default;
        return false;
    }

    public string GetStringValue()
    {
        return Value;
    }

    public int GetIntValue()
    {
        return int.Parse(Value);
    }

    public float GetFloatValue()
    {
        return float.Parse(Value);
    }

    public override string ToString()
    {
        return OriginalArgument;
    }
}

public sealed class MultiValueArgument : ICommandLineArgument
{
    public string OriginalArgument { get; }
    public string Name { get; }
    private string[] Value { get; }

    public MultiValueArgument(string InArgument)
    {
        OriginalArgument = InArgument;
        string[] Components = InArgument.Split("=");
        Name = Components[0].Replace("-", "");
        Value = Components[1].Split(",");
    }

    public bool TryGetValue<T>(out T? OutValue)
    {
        if (typeof(T) == typeof(string[]))
        {
            OutValue = (T)(object)GetStringArrayValue();
            return true;
        }

        if (typeof(T) == typeof(int[]))
        {
            OutValue = (T)(object)GetIntArrayValue();
            return true;
        }

        if (typeof(T) == typeof(float[]))
        {
            OutValue = (T)(object)GetFloatArrayValue();
            return true;
        }

        OutValue = default;
        return false;
    }

    public string[] GetStringArrayValue()
    {
        return Value;
    }

    public int[] GetIntArrayValue()
    {
        int[] OutValue = new int[Value.Length];
        for (int Index = 0; Index < OutValue.Length; Index++)
        {
            OutValue[Index] = int.Parse(Value[Index]);
        }
        return OutValue;
    }

    public float[] GetFloatArrayValue()
    {
        float[] OutValue = new float[Value.Length];
        for (int Index = 0; Index < OutValue.Length; Index++)
        {
            OutValue[Index] = float.Parse(Value[Index]);
        }
        return OutValue;
    }

    public override string ToString()
    {
        return OriginalArgument;
    }
}

public class InvalidCommandException(string InMessage) : ABaseException(InMessage);
public class UnknownArgumentException(string InMessage) : ABaseException(InMessage);