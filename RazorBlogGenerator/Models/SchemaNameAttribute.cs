using System.Text.RegularExpressions;

namespace RazorBlogGenerator.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class SchemaNameAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;

    public static string GetSchemaName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SchemaNameAttribute), false)
            .FirstOrDefault() as SchemaNameAttribute;

        return attr?.Name ?? ToKebabCase(type.Name);
    }

    private static string ToKebabCase(string name)
    {
        var cleaned = name.EndsWith("Model") ? name[..^5] : name;
        return KebabRegex().Replace(cleaned, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex KebabRegex();
}
