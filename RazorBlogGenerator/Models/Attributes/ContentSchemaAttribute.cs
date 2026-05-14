using CaseExtensions;

namespace RazorBlogGenerator.Models.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ContentSchemaAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;

    public static string GetSchemaName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(ContentSchemaAttribute), false)
            .FirstOrDefault() as ContentSchemaAttribute;

        return attr?.Name ?? type.Name.ToKebabCase();
    }
}
