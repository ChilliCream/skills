using System.Reflection;
using CookieCrumble;
using Skills.Extensions;
using Xunit;

namespace Skills.Tests;

/// <summary>
/// Locks the Skills library's public surface. A future change that widens or narrows what the
/// assembly exposes to an embedding host must update this snapshot, so the diff is visible to a
/// reviewer rather than happening as a side effect of an unrelated change.
/// </summary>
public class PublicSurfaceTests
{
    [Fact]
    public void Public_Surface_Matches_Snapshot()
    {
        var assembly = typeof(ServiceCollectionExtensions).Assembly;

        // IsVisible (rather than IsPublic/IsNestedPublic) accounts for the whole containment
        // chain: a type declared "public" but nested inside an internal type is not actually
        // reachable from outside the assembly, so it does not belong in the locked surface.
        var publicTypes = assembly.GetTypes()
            .Where(t => t.IsVisible && !t.IsSpecialName && t.Name[0] != '<')
            .OrderBy(t => t.FullName, StringComparer.Ordinal);

        var lines = new List<string>();
        foreach (var type in publicTypes)
        {
            lines.Add(DescribeType(type));
            foreach (var member in DescribeMembers(type))
            {
                lines.Add("    " + member);
            }
        }

        // The snapshot deliberately covers the whole assembly, not just one namespace: widening
        // the surface anywhere in Skills (a stray "public" on an unrelated type) must show up here.
        string.Join("\n", lines).MatchInlineSnapshot(
            """
            public sealed class Skills.Commands.SkillsCommand : System.CommandLine.Command, Skills.ICommandServicesSource
                public SkillsCommand(System.IServiceProvider serviceProvider)
            public static class Skills.Extensions.ServiceCollectionExtensions
                public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSkillsServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services, string toolCommandName)
            """);
    }

    private static string DescribeType(Type type)
    {
        var kind = type switch
        {
            { IsInterface: true } => "interface",
            { IsEnum: true } => "enum",
            { IsValueType: true } => "struct",
            { IsAbstract: true, IsSealed: true } => "static class",
            { IsSealed: true } => "sealed class",
            { IsAbstract: true } => "abstract class",
            _ => "class",
        };

        // A base type or an implemented interface is as much a contract as a member: swapping
        // SkillsCommand's base class, or dropping an interface it implements, changes what an
        // embedding host can do with the type without touching a single member signature.
        var contracts = new List<string>();
        var baseType = type.BaseType;
        if (baseType is not null && baseType != typeof(object) && baseType != typeof(ValueType) && baseType != typeof(Enum))
        {
            contracts.Add(TypeName(baseType));
        }

        // GetInterfaces() flattens the whole hierarchy, so subtract what the base type already
        // brings in to keep this line to the interfaces the type itself declares.
        var inherited = baseType?.GetInterfaces() ?? Type.EmptyTypes;
        contracts.AddRange(
            type.GetInterfaces()
                .Except(inherited)
                .Select(TypeName)
                .OrderBy(name => name, StringComparer.Ordinal));

        var suffix = contracts.Count > 0 ? " : " + string.Join(", ", contracts) : string.Empty;
        return $"public {kind} {type.FullName}{suffix}";
    }

    private static IEnumerable<string> DescribeMembers(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var ctor in type.GetConstructors(flags & ~BindingFlags.Static).OrderBy(c => Parameters(c), StringComparer.Ordinal))
        {
            yield return $"public {type.Name}({Parameters(ctor)})";
        }

        // IsSpecialName also marks operator overloads (op_Addition, op_Implicit, ...), not just
        // the compiler-generated property/event accessors this filter exists to drop. Keeping the
        // "op_" ones back in is what makes a public operator or conversion show up in the lock.
        foreach (var method in type.GetMethods(flags)
            .Where(m => !m.IsSpecialName || m.Name.StartsWith("op_", StringComparison.Ordinal))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => Parameters(m), StringComparer.Ordinal))
        {
            var modifier = method.IsStatic ? "static " : string.Empty;
            yield return $"public {modifier}{TypeName(method.ReturnType)} {method.Name}({Parameters(method)})";
        }

        foreach (var property in type.GetProperties(flags).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            // GetProperties(Public) returns the property if either accessor is public, so
            // CanRead/CanWrite alone can't tell "public get; internal set;" from "get; set;".
            // Render each accessor's own visibility instead of assuming it matches the property's.
            var accessors = DescribeAccessor(property.GetMethod, "get") + DescribeAccessor(property.SetMethod, "set");
            yield return $"public {TypeName(property.PropertyType)} {property.Name} {{ {accessors}}}";
        }

        foreach (var field in type.GetFields(flags).Where(f => !f.IsSpecialName).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            yield return $"public {TypeName(field.FieldType)} {field.Name}";
        }

        foreach (var evt in type.GetEvents(flags).OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            yield return $"public event {TypeName(evt.EventHandlerType!)} {evt.Name}";
        }
    }

    private static string DescribeAccessor(MethodInfo? accessor, string keyword)
    {
        if (accessor is null)
        {
            return string.Empty;
        }

        return $"{AccessorVisibility(accessor)}{keyword}; ";
    }

    private static string AccessorVisibility(MethodInfo accessor) => accessor switch
    {
        { IsPublic: true } => string.Empty,
        { IsFamilyOrAssembly: true } => "protected internal ",
        { IsFamily: true } => "protected ",
        { IsAssembly: true } => "internal ",
        { IsFamilyAndAssembly: true } => "private protected ",
        _ => "private ",
    };

    private static string Parameters(MethodBase method) =>
        string.Join(", ", method.GetParameters().Select(p => $"{TypeName(p.ParameterType)} {p.Name}"));

    private static string TypeName(Type type)
    {
        // Generic method/type parameters (T, TOption, ...) have no Namespace/FullName worth
        // qualifying; they are placeholders, not real types.
        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsGenericType)
        {
            var genericName = QualifiedName(type.Name[..type.Name.IndexOf('`')], type);
            var arguments = string.Join(", ", type.GetGenericArguments().Select(TypeName));
            return $"{genericName}<{arguments}>";
        }

        return type.Name switch
        {
            "Void" => "void",
            "String" => "string",
            "Boolean" => "bool",
            "Int32" => "int",
            _ => QualifiedName(type.Name, type),
        };
    }

    // Type.Name alone collapses namespaces: two types that share a short name in different
    // namespaces would render identically, and a type moved to a new namespace would not show
    // up as a change. Prefixing with Namespace keeps that identity visible in the snapshot.
    private static string QualifiedName(string name, Type type) =>
        type.Namespace is null ? name : $"{type.Namespace}.{name}";
}
