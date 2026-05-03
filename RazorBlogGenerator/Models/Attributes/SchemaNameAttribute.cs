using CaseExtensions;

namespace RazorBlogGenerator.Models.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SchemaNameAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;

    public static string GetSchemaName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(SchemaNameAttribute), false)
            .FirstOrDefault() as SchemaNameAttribute;

        return attr?.Name ?? type.Name.ToKebabCase();
    }
}
