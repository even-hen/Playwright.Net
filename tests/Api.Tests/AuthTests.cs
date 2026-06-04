using NUnit.Framework;
using System;
using System.Threading.Tasks;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("auth")]
public class AuthTests : ApiBaseTest
{
    private AuthClient _authClient = null!;
    private static SupabaseDataManager _dataManager = null!;
    private string? _registeredUserId;

    private static string _testUserEmail = null!;
    private static string _testUserPassword = null!;
    private static string _testUserId = null!;

    [OneTimeSetUp]
    public static async Task SeedAuthTestData()
    {
        _dataManager = new SupabaseDataManager();
        _testUserEmail = $"authtest{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        _testUserPassword = ConfigManager.Settings.DefaultPassword;

        // Seed the user in Supabase auth and profiles
        _testUserId = await _dataManager.CreateAuthUserAdminAsync(_testUserEmail, _testUserPassword, "Auth Test User");
        await _dataManager.CreateUserProfileAsync(_testUserId, _testUserEmail, "Auth Test User", "Adult", 100, null);
    }

    [OneTimeTearDown]
    public static async Task CleanupAuthTestData()
    {
        if (!string.IsNullOrEmpty(_testUserId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_testUserId);
        }
    }

    [SetUp]
    public void SetUpClients()
    {
        _authClient = new AuthClient(ApiRequestContext);
        _registeredUserId = null;
    }

    [TearDown]
    public async Task CleanupCreatedUsers()
    {
        if (!string.IsNullOrEmpty(_registeredUserId))
        {
            try
            {
                await _dataManager.DeleteGroupCascadeAsync(_registeredUserId);
            }
            catch { /* Ignore if it was not associated or already deleted */ }
        }
    }

    [Test]
    [Category("critical")]
    public async Task TC_AUTH_001_SuccessfulRegistration()
    {
        var testEmail = $"newuser{Guid.NewGuid().ToString().Substring(0, 8)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Register user via API client
        var authResponse = await _authClient.SignUpAsync(testEmail, password, new { name = "New User" });
        _registeredUserId = authResponse.User.Id;

        Assert.Multiple((Action)(() =>
        {
            Assert.That(authResponse.User, Is.Not.Null);
            Assert.That(authResponse.User.Email, Is.EqualTo(testEmail));
            Assert.That(authResponse.AccessToken, Is.Not.Null.Or.Empty);
        }));

        // Create the user profile manually in public.users to match frontend behavior
        await _dataManager.CreateUserProfileAsync(authResponse.User.Id, testEmail, "New User", "Adult", 80, null);

        // Verify the profile was created in public.users
        var usersClient = new UsersClient(ApiRequestContext);
        var profile = await usersClient.GetUserProfileSingleAsync(authResponse.User.Id);
        
        Assert.Multiple((Action)(() =>
        {
            Assert.That(profile.Email, Is.EqualTo(testEmail));
            Assert.That(profile.Name, Is.EqualTo("New User"));
            Assert.That(profile.Type, Is.EqualTo("Adult")); // Default signup role
        }));
    }

    [Test]
    [Category("critical")]
    public async Task TC_AUTH_010_SuccessfulLogin()
    {
        // Verify login
        var authResponse = await _authClient.SignInAsync(_testUserEmail, _testUserPassword);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(authResponse.User, Is.Not.Null);
            Assert.That(authResponse.User.Email, Is.EqualTo(_testUserEmail));
            Assert.That(authResponse.AccessToken, Is.Not.Null.Or.Empty);
        }));
    }

    [Test]
    public void TC_AUTH_011_LoginFails_WrongPassword()
    {
        var password = "WrongPassword123!";

        var ex = Assert.ThrowsAsync<Exception>((Func<Task>)(async () =>
        {
            await _authClient.SignInAsync(_testUserEmail, password);
        }));

        Assert.That(ex!.Message, Does.Contain("400")
            .Or.Contain("401")
            .Or.Contain("credentials"));
    }

    [Test]
    public void TC_AUTH_012_LoginFails_NonExistentEmail()
    {
        var email = "nobody_exists_here_123@test.com";
        var password = "AnyPassword!";

        var ex = Assert.ThrowsAsync<Exception>((Func<Task>)(async () =>
        {
            await _authClient.SignInAsync(email, password);
        }));

        Assert.That(ex!.Message, Does.Contain("400")
            .Or.Contain("401")
            .Or.Contain("credentials"));
    }

    [Test]
    public async Task TC_AUTH_020_SuccessfulSignOut()
    {
        // Login first
        var authResponse = await _authClient.SignInAsync(_testUserEmail, _testUserPassword);
        
        // Setup authenticated client context
        var authHeaders = new System.Collections.Generic.Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {authResponse.AccessToken}" }
        };
        var authContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = authHeaders
        });
        
        var authenticatedAuthClient = new AuthClient(authContext);
        
        // Sign out
        await authenticatedAuthClient.SignOutAsync();
        
        // Subsequent calls to Auth API with that token should fail or return unauthorized (401 or 403)
        var ex = Assert.ThrowsAsync<Exception>((Func<Task>)(async () =>
        {
            var userResponse = await authContext.GetAsync("/auth/v1/user");
            if (!userResponse.Ok)
            {
                throw new Exception($"Status {userResponse.Status}");
            }
        }));

        Assert.That(ex!.Message, Does.Contain("401").Or.Contain("403").Or.Contain("unauthorized").Or.Contain("forbidden").Or.Contain("Status 401").Or.Contain("Status 403"));
    }
/* Supabase email rate limit exceeded
    [Test]
    public async Task TC_AUTH_030_ResetPasswordRequest()
    {
        // Should succeed without error (Supabase doesn't throw even for fake emails to prevent user enumeration)
        await _authClient.RequestPasswordRecoveryAsync(_testUserEmail);
        Assert.Pass("Password reset link sent successfully.");
    }
*/
}
