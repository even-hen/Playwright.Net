using NJsonSchema;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace PlaywrightFramework.Tools.SchemaValidator;

public static class JsonSchemaValidator
{
    private static readonly Dictionary<string, JsonSchema> _schemas = new(StringComparer.OrdinalIgnoreCase);

    static JsonSchemaValidator()
    {
        try
        {
            // Find _docs/swagger.json by walking up the directory tree
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string? swaggerPath = null;
            
            while (currentDir != null)
            {
                var docPath = Path.Combine(currentDir.FullName, "_docs", "swagger.json");
                if (File.Exists(docPath))
                {
                    swaggerPath = docPath;
                    break;
                }
                currentDir = currentDir.Parent;
            }

            if (string.IsNullOrEmpty(swaggerPath) || !File.Exists(swaggerPath))
            {
                throw new FileNotFoundException($"Could not locate _docs/swagger.json walking up from: {AppDomain.CurrentDomain.BaseDirectory}");
            }

            var swaggerJson = File.ReadAllText(swaggerPath);
            
            // Extract components/schemas using JsonDocument
            using var document = JsonDocument.Parse(swaggerJson);
            if (!document.RootElement.TryGetProperty("components", out var components) ||
                !components.TryGetProperty("schemas", out var schemas))
            {
                throw new Exception("No components/schemas found in swagger.json!");
            }

            var schemasRawJson = schemas.GetRawText();
            
            // Wrap in a standard JSON Schema structure with definitions
            var definitionsJson = $"{{\"$schema\": \"http://json-schema.org/draft-07/schema#\", \"definitions\": {schemasRawJson}}}";
            
            // Replace internal OpenAPI references with standard JSON schema definitions references
            definitionsJson = definitionsJson.Replace("#/components/schemas/", "#/definitions/");
            
            var parentSchema = JsonSchema.FromJsonAsync(definitionsJson).GetAwaiter().GetResult();
            
            foreach (var pair in parentSchema.Definitions)
            {
                _schemas[pair.Key] = pair.Value;
            }
            
            Console.WriteLine($"Successfully loaded {_schemas.Count} schemas from swagger.json: {string.Join(", ", _schemas.Keys)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing JsonSchemaValidator: {ex.Message}");
            throw;
        }
    }

    public static SchemaValidationResult Validate(string jsonResponse, string schemaName)
    {
        if (!_schemas.TryGetValue(schemaName, out var schema))
        {
            return new SchemaValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Schema '{schemaName}' not found in swagger.json." }
            };
        }

        var trimmed = jsonResponse.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var allErrors = new List<string>();
                    int index = 0;
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        var errors = schema.Validate(item.GetRawText());
                        allErrors.AddRange(errors.Select(e => $"[{index}]{e.Path}: {e.Kind} - {e.Property}"));
                        index++;
                    }
                    return new SchemaValidationResult
                    {
                        IsValid = !allErrors.Any(),
                        Errors = allErrors
                    };
                }
            }
            catch (Exception ex)
            {
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Errors = new List<string> { $"Failed to parse response as JSON array: {ex.Message}" }
                };
            }
        }

        var directErrors = schema.Validate(jsonResponse);
        return new SchemaValidationResult
        {
            IsValid = !directErrors.Any(),
            Errors = directErrors.Select(e => $"{e.Path}: {e.Kind} - {e.Property}").ToList()
        };
    }
}

public class SchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string ErrorSummary => IsValid ? string.Empty : string.Join("; ", Errors);
}
