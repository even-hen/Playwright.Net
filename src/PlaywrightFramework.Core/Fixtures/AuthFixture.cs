using Microsoft.Playwright;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Core.Configuration;

namespace PlaywrightFramework.Core.Fixtures;

public static class AuthFixture
{
    private static readonly string AuthStatesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "auth-states");

    public static async Task<string> GetStorageStatePathAsync(string email, string password)
    {
        Directory.CreateDirectory(AuthStatesDir);
        var sanitizedEmail = email.Replace("@", "_").Replace(".", "_");
        var stateFilePath = Path.Combine(AuthStatesDir, $"{sanitizedEmail}.json");

        // If the state file already exists, reuse it to save time
        if (File.Exists(stateFilePath))
        {
            // Simple validation: check if the file was created recently (e.g. less than 1 hour ago)
            var lastWrite = File.GetLastWriteTime(stateFilePath);
            if ((DateTime.Now - lastWrite).TotalHours < 1)
            {
                return stateFilePath;
            }
        }

        // Programmatically sign in and generate the StorageState JSON
        await GenerateStorageStateFileAsync(email, password, stateFilePath);
        return stateFilePath;
    }

    private static async Task GenerateStorageStateFileAsync(string email, string password, string outputPath)
    {
        var settings = ConfigManager.Settings;
        using var client = new HttpClient();
        
        client.DefaultRequestHeaders.Add("apikey", settings.SupabaseAnonKey);
        
        var requestBody = new
        {
            email = email,
            password = password
        };
        
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var url = $"{settings.ApiBaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password";

        var response = await client.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to authenticate user {email} via Supabase API. Status: {response.StatusCode}, Error: {errContent}");
        }

        var responseJsonStr = await response.Content.ReadAsStringAsync();
        
        // Supabase localStorage key format
        var supabaseUrlUri = new Uri(settings.ApiBaseUrl);
        var refId = settings.ApiBaseUrl.Split('.')[0].Replace("https://", "");
        var localStorageKey = $"sb-{refId}-auth-token";

        // Build Playwright StorageState JSON structure
        var baseUrlUri = new Uri(settings.BaseUrl);
        var origin = baseUrlUri.GetLeftPart(UriPartial.Authority);

        var storageState = new
        {
            cookies = new object[] { },
            origins = new[]
            {
                new
                {
                    origin = origin,
                    localStorage = new[]
                    {
                        new
                        {
                            name = localStorageKey,
                            value = responseJsonStr
                        }
                    }
                }
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var jsonStr = JsonSerializer.Serialize(storageState, options);
        await File.WriteAllTextAsync(outputPath, jsonStr);
    }
}
