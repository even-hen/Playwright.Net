using Microsoft.Playwright;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightFramework.Api.Clients;

public abstract class BaseApiClient
{
    protected readonly IAPIRequestContext RequestContext;

    protected BaseApiClient(IAPIRequestContext requestContext)
    {
        RequestContext = requestContext;
    }

    public static Action<string, string>? SchemaValidatorHook { get; set; }

    protected async Task<T> HandleResponseAsync<T>(IAPIResponse response, string? schemaName = null)
    {
        var content = await response.TextAsync();
        if (!response.Ok)
        {
            throw new Exception($"API request failed with status {response.Status}: {content}");
        }

        if (!string.IsNullOrEmpty(schemaName) && SchemaValidatorHook != null)
        {
            SchemaValidatorHook(content, schemaName);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new Exception("Deserialization returned null.");
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to deserialize response body to {typeof(T).Name}. Body: {content}. Error: {ex.Message}");
        }
    }

    protected async Task EnsureSuccessAsync(IAPIResponse response)
    {
        if (!response.Ok)
        {
            var content = await response.TextAsync();
            throw new Exception($"API request failed with status {response.Status}: {content}");
        }
    }
}
