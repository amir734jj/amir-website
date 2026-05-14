using NJsonSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorBlogGenerator.Models.Attributes;
using Serilog;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RazorBlogGenerator;

public static class YamlValidator
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly Dictionary<string, string> ModelToSchemaMap =
        typeof(ContentSchemaAttribute).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(ContentSchemaAttribute), false).Length > 0)
            .ToDictionary(t => t.Name, ContentSchemaAttribute.GetSchemaName);

    public static async Task<int> ValidateAsync(string dataDir, string schemasDir)
    {
        var schemaMap = new Dictionary<string, JsonSchema>();
        foreach (var file in Directory.GetFiles(schemasDir, "*.schema.json"))
        {
            var name = Path.GetFileNameWithoutExtension(file).Replace(".schema", "");
            var schema = await JsonSchema.FromFileAsync(file);
            schemaMap[name] = schema;
        }

        var errorCount = 0;

        foreach (var file in Directory.GetFiles(dataDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)))
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("site.yaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("site.yml", StringComparison.OrdinalIgnoreCase))
            {
                errorCount += ValidateFile(file, schemaMap.GetValueOrDefault("site-config"));
                continue;
            }

            var yaml = await File.ReadAllTextAsync(file);

            var meta = YamlDeserializer.Deserialize<Dictionary<string, object>>(yaml);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (meta is null || !meta.TryGetValue("model", out var modelObj))
            {
                Log.Warning("{File}: missing 'model' field, skipping", file);
                errorCount++;
                continue;
            }

            var modelName = modelObj.ToString()!;
            if (!ModelToSchemaMap.TryGetValue(modelName, out var schemaName))
            {
                Log.Warning("{File}: no schema mapping for model '{Model}'", file, modelName);
                errorCount++;
                continue;
            }

            if (!schemaMap.TryGetValue(schemaName, out var schema))
            {
                Log.Warning("{File}: no schema found for '{SchemaName}'", file, schemaName);
                errorCount++;
                continue;
            }

            errorCount += ValidateYamlAgainstSchema(file, yaml, schema);
        }

        if (errorCount == 0)
        {
            Log.Information("All YAML files are valid");
        }
        else
        {
            Log.Error("{Count} validation error(s) found", errorCount);
        }

        return errorCount;
    }

    private static int ValidateFile(string file, JsonSchema? schema)
    {
        if (schema == null)
        {
            Log.Warning("{File}: no schema found", file);
            return 1;
        }

        var yaml = File.ReadAllText(file);
        return ValidateYamlAgainstSchema(file, yaml, schema);
    }

    private static JToken YamlNodeToJToken(YamlNode node)
    {
        return node switch
        {
            YamlMappingNode mapping => new JObject(
                mapping.Children.Select(kv =>
                    new JProperty(((YamlScalarNode)kv.Key).Value!, YamlNodeToJToken(kv.Value)))),
            YamlSequenceNode sequence => new JArray(sequence.Children.Select(YamlNodeToJToken)),
            YamlScalarNode scalar => ScalarToJValue(scalar),
            _ => JValue.CreateNull()
        };
    }

    private static JValue ScalarToJValue(YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (value is null)
        {
            return JValue.CreateNull();
        }

        // Quoted scalars are explicitly typed as strings in YAML — never coerce them.
        if (scalar.Style != ScalarStyle.Plain)
        {
            return new JValue(value);
        }

        return value switch
        {
            // Plain scalars: coerce only the types the schemas actually use.
            "true" or "True" or "TRUE" => new JValue(true),
            "false" or "False" or "FALSE" => new JValue(false),
            "null" or "Null" or "NULL" or "~" => JValue.CreateNull(),
            _ => new JValue(value)
        };
    }

    private static int ValidateYamlAgainstSchema(string file, string yaml, JsonSchema schema)
    {
        var yamlStream = new YamlStream();
        yamlStream.Load(new StringReader(yaml));
        var json = YamlNodeToJToken(yamlStream.Documents[0].RootNode).ToString(Formatting.None);

        var validationErrors = schema.Validate(json);
        if (validationErrors.Count == 0)
        {
            Log.Information("{File}: valid", file);
            return 0;
        }

        foreach (var error in validationErrors)
        {
            Log.Error("{File}: {Kind} at {Path}", file, error.Kind, error.Path);
        }

        return validationErrors.Count;
    }
}
