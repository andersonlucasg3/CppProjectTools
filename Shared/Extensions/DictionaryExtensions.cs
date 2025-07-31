namespace Shared.Extensions;

public static class DictionaryExtensions
{
    public static void AddFrom<TKey, TValue>(this Dictionary<TKey, TValue> Dict, params IReadOnlyDictionary<TKey, TValue>[] Others)
        where TKey : notnull
    {
        foreach (IReadOnlyDictionary<TKey, TValue> Other in Others)
        {
            foreach (KeyValuePair<TKey, TValue> Pair in Other)
            {
                Dict.Add(Pair.Key, Pair.Value);
            }
        }
    }
}