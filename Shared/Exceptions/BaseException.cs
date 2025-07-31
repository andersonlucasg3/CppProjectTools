namespace Shared.Exceptions;

public abstract class ABaseException : Exception
{
    protected ABaseException() : base()
    {
        //
    }

    protected ABaseException(string? message) : base(message)
    {
        //
    }
}
