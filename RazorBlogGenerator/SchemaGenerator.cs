using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;
using RazorBlogGenerator.Models.Attributes;
using Serilog;

namespace RazorBlogGenerator;

public static class SchemaGenerator
{
    private static readonly NewtonsoftJsonSchemaGeneratorSettings Settings = new()
    {
        SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        }
    };

    public static async Task GenerateAsync(string schemasDir)
    {
        Directory.CreateDirectory(schemasDir);

        var types = typeof(SchemaNameAttribute).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(SchemaNameAttribute), false).Length > 0)
            .ToDictionary(SchemaNameAttribute.GetSchemaName, t => t);

        foreach (var (name, type) in types)
        {
            var schema = NewtonsoftJsonSchemaGenerator.FromType(type, Settings);
            AllowAdditionalEverywhere(schema);

            var json = schema.ToJson();

            var path = Path.Combine(schemasDir, $"{name}.schema.json");
            await File.WriteAllTextAsync(path, json);
            Log.Information("Generated schema {Name} from {Type}", path, type.Name);
        }
    }

    private static void AllowAdditionalEverywhere(JsonSchema schema)
    {
        schema.AllowAdditionalProperties = true;
        foreach (var def in schema.Definitions.Values)
        {
            AllowAdditionalEverywhere(def);
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (schema.AllOf != null)
        {
            foreach (var item in schema.AllOf)
            {
                AllowAdditionalEverywhere(item);
            }
        }
    }
}
