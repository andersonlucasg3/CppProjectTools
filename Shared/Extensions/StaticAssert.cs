using System.Diagnostics;

namespace Shared.Exceptions;

public static class AssertsExtensions
{
    [Conditional("DEBUG")]
    public static void RuntimeAssert(this object _, bool InCondition, string InMessage)
    {
        if (!InCondition) throw new InvalidOperationException(InMessage);
    }
}