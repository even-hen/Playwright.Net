using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.UI.Pages;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace E2E.Tests.Tabs;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("e2e")]
[Category("notifications")]
public class NotificationsTabTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string _groupId = null!;
    private string _userId = null!;
    private string _userEmail = null!;

    [SetUp]
    public async Task SetUpData()
    {
        _dataManager = new SupabaseDataManager();
        _userEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Create family group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Notifications House", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = password, Name = "Papa Notification", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _userId = setup.UserIds[0];

        // Seed two unread notifications for this user via Supabase API
        var clientHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseServiceRoleKey },
            { "Authorization", $"Bearer {ConfigManager.Settings.SupabaseServiceRoleKey}" }
        };
        var apiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = clientHeaders
        });

        var notif1 = new
        {
            user_id = _userId,
            group_id = _groupId,
            type = "daily_summary",
            title = "Task Reminder",
            body = "Don't forget to wash the dishes today!",
            is_read = false
        };

        var notif2 = new
        {
            user_id = _userId,
            group_id = _groupId,
            type = "daily_summary",
            title = "Chore Done",
            body = "Child completed sweep the deck",
            is_read = false
        };

        var res1 = await apiContext.PostAsync("/rest/v1/notifications", new() { DataObject = notif1 });
        Assert.That(res1.Ok, Is.True, $"Failed to post notif1: {await res1.TextAsync()}");

        var res2 = await apiContext.PostAsync("/rest/v1/notifications", new() { DataObject = notif2 });
        Assert.That(res2.Ok, Is.True, $"Failed to post notif2: {await res2.TextAsync()}");

        // Get StorageState
        var statePath = await AuthFixture.GetStorageStatePathAsync(_userEmail, password);
        await SetupCustomContextAsync(new() { StorageStatePath = statePath });
    }

    [TearDown]
    public async Task CleanupData()
    {
        if (!string.IsNullOrEmpty(_groupId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_groupId);
        }
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_030_ViewNotificationsList_And_UnreadDot()
    {
        var tab = new TabBar(CurrentPage);
        var notificationsPage = new NotificationsPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToAlertsAsync();

        // Verify notifications list contains the seeded notifications
        await Expect(CurrentPage.GetByText("Task Reminder")).ToBeVisibleAsync();
        await Expect(CurrentPage.GetByText("Chore Done")).ToBeVisibleAsync();

        // Verify unread count indicator or header is present
        await Expect(notificationsPage.UnreadCountHeader).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_E2E_031_MarkAllNotificationsAsRead()
    {
        var tab = new TabBar(CurrentPage);
        var notificationsPage = new NotificationsPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToAlertsAsync();

        // Verify initially we have items
        await Expect(CurrentPage.GetByText("Task Reminder")).ToBeVisibleAsync();
        await Expect(notificationsPage.UnreadCountHeader).ToBeVisibleAsync();

        // Click mark all read
        await notificationsPage.ClickMarkAllReadAsync();

        // After marking all read, there should be no unread count, or the unread header is hidden/empty
        await Expect(notificationsPage.UnreadCountHeader).Not.ToBeVisibleAsync(new() { Timeout = 5000 });
    }
}