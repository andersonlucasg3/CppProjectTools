using System.Reflection;

namespace ProjectTools.Reflections;

public static class TypeFinder
{
    public static Type[] FindChildClasses<T>(Assembly[]? Assemblies = null)
    {
        Assemblies ??= AppDomain.CurrentDomain.GetAssemblies()
            .Where(Each => !(Each.FullName?.StartsWith("System.") ?? false))
            .Where(Each => !(Each.FullName?.StartsWith("Microsoft.") ?? false)).ToArray();

        Type[] Types = Assemblies.SelectMany(Each =>
        {
            Type[] PossibleTypes = Each.GetTypes();
            return PossibleTypes
                .Where(EachT => EachT is { IsClass: true, IsAbstract: false } && EachT.IsAssignableTo(typeof(T)));
        }).ToArray();
        
        return Types;
    }
}

