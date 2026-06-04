using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Users;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("users")]
public class UserTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _groupId = null!;
    private static string _adultId = null!;
    private static string _teenId = null!;
    private static string _adultToken = null!;
    private static string _teenToken = null!;
    
    private UsersClient _adultUsersClient = null!;
    private UsersClient _teenUsersClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var teenEmail = $"teen_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seeding standard group with an Adult and a Teen
        var setup = await _dataManager.CreateTestGroupSetupAsync("Test Smiths", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = adultEmail, Password = password, Name = "John Smith", Type = "Adult", Resource = 100 },
            new UserSetupSpec { Email = teenEmail, Password = password, Name = "Alex Smith", Type = "Teen", Resource = 60 }
        });

        _groupId = setup.GroupId;
        _adultId = setup.UserIds[0];
        _teenId = setup.UserIds[1];

        // Retrieve tokens
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var headers = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Content-Type", "application/json" }
        };
        var tempContext = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = headers
        });
        var authClient = new AuthClient(tempContext);
        var adultAuth = await authClient.SignInAsync(adultEmail, password);
        _adultToken = adultAuth.AccessToken;

        var teenAuth = await authClient.SignInAsync(teenEmail, password);
        _teenToken = teenAuth.AccessToken;
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDownCleanup()
    {
        if (!string.IsNullOrEmpty(_groupId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_groupId);
        }
    }

    [SetUp]
    public async Task SetUpClientContexts()
    {
        var adultHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_adultToken}" }
        };
        var adultContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = adultHeaders
        });
        _adultUsersClient = new UsersClient(adultContext);

        var teenHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_teenToken}" }
        };
        var teenContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = teenHeaders
        });
        _teenUsersClient = new UsersClient(teenContext);
    }

    [Test]
    [Category("critical")]
    public async Task TC_USER_001_FetchUserProfileById()
    {
        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_adultId);
        
        Assert.Multiple((Action)(() =>
        {
            Assert.That(profile.Id, Is.EqualTo(_adultId));
            Assert.That(profile.Name, Is.EqualTo("John Smith"));
            Assert.That(profile.Type, Is.EqualTo("Adult"));
            Assert.That(profile.Resource, Is.EqualTo(100));
        }));
    }

    [Test]
    public async Task TC_USER_003_FetchAllUsersInGroup()
    {
        var profiles = await _adultUsersClient.GetUserProfilesAsync(groupId: _groupId);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(profiles.Count, Is.EqualTo(2));
            Assert.That(profiles.Exists(p => p.Id == _adultId), Is.True);
            Assert.That(profiles.Exists(p => p.Id == _teenId), Is.True);
        }));
    }

    [Test]
    public async Task TC_USER_010_UpdateUserName_AdultUpdatesSelf()
    {
        var newName = "John Smith Updated";
        await _adultUsersClient.UpdateUserProfileAsync(_adultId, new { name = newName });

        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_adultId);
        Assert.That(profile.Name, Is.EqualTo(newName));
    }

    [Test]
    public async Task TC_USER_012_UpdateUserResource_AdultUpdatesSelf()
    {
        await _adultUsersClient.UpdateUserProfileAsync(_adultId, new { resource = 80 });

        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_adultId);
        Assert.That(profile.Resource, Is.EqualTo(80));
    }

    [Test]
    public async Task TC_USER_013_UpdateUserType_AdultUpdatesTeenToChild()
    {
        // Adult can update other members in their group
        await _adultUsersClient.UpdateUserProfileAsync(_teenId, new { type = "Child" });

        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_teenId);
        Assert.That(profile.Type, Is.EqualTo("Child"));
    }

    [Test]
    public async Task TC_USER_014_UpdateNotificationTime()
    {
        await _adultUsersClient.UpdateUserProfileAsync(_adultId, new { notification_time = "18:00" });

        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_adultId);
        Assert.That(profile.NotificationTime, Is.EqualTo("18:00"));
    }

    [Test]
    public async Task TC_USER_015_UpdateTimezone()
    {
        var newTz = "Asia/Tokyo";
        await _adultUsersClient.UpdateUserProfileAsync(_adultId, new { timezone = newTz });

        var profile = await _adultUsersClient.GetUserProfileSingleAsync(_adultId);
        Assert.That(profile.Timezone, Is.EqualTo(newTz));
    }
}
