namespace Shared.Extensions;

public static class StringExtensions
{
    public static string Quoted(this string InValue) => $"\"{InValue}\"";
}
