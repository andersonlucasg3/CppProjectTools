namespace Shared.Extensions;

public static class EnumExtensions
{
    public static TEnum ToEnum<TEnum>(this string Value) where TEnum : struct
    {
        return Enum.Parse<TEnum>(Value);
    }

    public static bool TryToEnum<TEnum>(this string Value, out TEnum OutValue) where TEnum : struct
    {
        return Enum.TryParse(Value, out OutValue);
    }
}