using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Notifications;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("notifications")]
public class NotificationTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _groupId = null!;
    private static string _adultId = null!;
    private static string _adultToken = null!;
    
    private NotificationsClient _notificationsClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seed a family group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Notification Test Smiths", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = adultEmail, Password = password, Name = "John Notifier", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _adultId = setup.UserIds[0];

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
    public async Task SetUpClientContext()
    {
        var headers = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_adultToken}" }
        };
        var context = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = headers
        });
        _notificationsClient = new NotificationsClient(context);
    }

    [Test]
    [Category("critical")]
    public async Task TC_NOTIF_001_MarkNotificationAsRead()
    {
        // Programmatically insert a notification using Data Manager
        // (we can seed notifications using Rest API via notifications endpoint)
        var clientHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseServiceRoleKey },
            { "Authorization", $"Bearer {ConfigManager.Settings.SupabaseServiceRoleKey}" },
            { "Prefer", "return=representation" }
        };
        var context = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = clientHeaders
        });
        
        var body = new
        {
            user_id = _adultId,
            group_id = _groupId,
            type = "daily_summary",
            title = "Welcome!",
            body = "Welcome to Nest",
            is_read = false
        };

        var postResponse = await context.PostAsync("/rest/v1/notifications", new()
        {
            DataObject = body
        });
        
        Assert.That(postResponse.Ok, Is.True);
        
        var listText = await postResponse.TextAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(listText);
        var notifId = doc.RootElement[0].GetProperty("id").GetString()!;

        // Fetch notifications to confirm it exists
        var notifs = await _notificationsClient.GetNotificationsAsync(_adultId);
        Assert.That(notifs.Exists(n => n.Id == notifId), Is.True);

        // Update read status to true
        await _notificationsClient.UpdateNotificationReadStateAsync(notifId, true);

        // Fetch again and verify
        var notifsUpdated = await _notificationsClient.GetNotificationsAsync(_adultId);
        var targetNotif = notifsUpdated.Find(n => n.Id == notifId);
        
        Assert.That(targetNotif, Is.Not.Null);
        Assert.That(targetNotif!.IsRead, Is.True);
    }
}
