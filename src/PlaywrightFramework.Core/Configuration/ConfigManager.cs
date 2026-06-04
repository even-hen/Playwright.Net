using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace PlaywrightFramework.Core.Configuration;

public static class ConfigManager
{
    private static readonly TestSettings _settings;

    static ConfigManager()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;

        // Load .env file by searching upwards recursively
        var dir = new DirectoryInfo(basePath);
        while (dir != null)
        {
            var envPath = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadAllLines(envPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    var index = line.IndexOf('=');
                    if (index > 0)
                    {
                        var key = line.Substring(0, index).Trim();
                        var value = line.Substring(index + 1).Trim();
                        // Remove surrounding quotes if present
                        if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }
                break;
            }
            dir = dir.Parent;
        }

        // Try getting environment from Environment variable (e.g. CI, Development, etc.)
        var env = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Development";
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("Configuration/appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"Configuration/appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        _settings = new TestSettings();
        configuration.GetSection("TestSettings").Bind(_settings);
        
        // Override with specific environment variable overrides if they exist
        var baseOverride = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrEmpty(baseOverride)) _settings.BaseUrl = baseOverride;

        var apiOverride = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrEmpty(apiOverride)) _settings.ApiBaseUrl = apiOverride;
        
        var serviceRoleOverride = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
        if (!string.IsNullOrEmpty(serviceRoleOverride)) _settings.SupabaseServiceRoleKey = serviceRoleOverride;
        
        var anonOverride = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
        if (!string.IsNullOrEmpty(anonOverride)) _settings.SupabaseAnonKey = anonOverride;
    }

    public static TestSettings Settings => _settings;
}
