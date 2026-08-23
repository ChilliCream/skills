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
            public sealed class Skills.Commands.SkillsCommand
                public SkillsCommand(IServiceProvider serviceProvider)
            public static class Skills.Extensions.ServiceCollectionExtensions
                public static IServiceCollection AddSkillsServices(IServiceCollection services, string toolCommandName)
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

        return $"public {kind} {type.FullName}";
    }

    private static IEnumerable<string> DescribeMembers(Type type)
    {
        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var ctor in type.GetConstructors(flags & ~BindingFlags.Static).OrderBy(c => Parameters(c), StringComparer.Ordinal))
        {
            yield return $"public {type.Name}({Parameters(ctor)})";
        }

        foreach (var method in type.GetMethods(flags)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => Parameters(m), StringComparer.Ordinal))
        {
            var modifier = method.IsStatic ? "static " : string.Empty;
            yield return $"public {modifier}{TypeName(method.ReturnType)} {method.Name}({Parameters(method)})";
        }

        foreach (var property in type.GetProperties(flags).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var accessors = (property.CanRead ? "get; " : string.Empty) + (property.CanWrite ? "set; " : string.Empty);
            yield return $"public {TypeName(property.PropertyType)} {property.Name} {{ {accessors}}}";
        }

        foreach (var field in type.GetFields(flags).Where(f => !f.IsSpecialName).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            yield return $"public {TypeName(field.FieldType)} {field.Name}";
        }
    }

    private static string Parameters(MethodBase method) =>
        string.Join(", ", method.GetParameters().Select(p => $"{TypeName(p.ParameterType)} {p.Name}"));

    private static string TypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name switch
            {
                "Void" => "void",
                "String" => "string",
                "Boolean" => "bool",
                "Int32" => "int",
                _ => type.Name,
            };
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = string.Join(", ", type.GetGenericArguments().Select(TypeName));
        return $"{name}<{arguments}>";
    }
}
