namespace PlaywrightFramework.Core.Configuration;

public class TestSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string SupabaseAnonKey { get; set; } = string.Empty;
    public string SupabaseServiceRoleKey { get; set; } = string.Empty;
    public bool Headless { get; set; }
    public int TimeoutSeconds { get; set; }
    public string DefaultPassword { get; set; } = string.Empty;
}
