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
[Category("settings")]
public class SettingsTabTests : UiBaseTest
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

        // Create standard group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Settings House", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = password, Name = "Mama Settings", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _userId = setup.UserIds[0];

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
    public async Task TC_E2E_032_ChangeNotificationTimeAndTheme()
    {
        var tab = new TabBar(CurrentPage);
        var settingsPage = new SettingsPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToSettingsAsync();

        // 1. Change notification time preference
        await settingsPage.ChangeNotificationTimeAsync("08:00");
        await Expect(settingsPage.NotificationTimeDropdown).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("08:00"));

        // 2. Switch theme mode
        await settingsPage.SwitchThemeAsync("dark");
        // Theme button click executes without throwing
        await settingsPage.SwitchThemeAsync("light");
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_033_GenerateInviteCode()
    {
        var tab = new TabBar(CurrentPage);
        var settingsPage = new SettingsPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToSettingsAsync();

        // Generate and parse invite code from dialog
        var code = await settingsPage.GenerateInviteCodeAsync();
        
        Assert.Multiple((Action)(() =>
        {
            Assert.That(code, Is.Not.Null.Or.Empty);
            Assert.That(code.Length, Is.EqualTo(8)); // Expected 8-char alphanumeric invite token
        }));
    }

    [Test]
    public async Task TC_E2E_034_LeaveGroup_And_SignOut()
    {
        var tab = new TabBar(CurrentPage);
        var settingsPage = new SettingsPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToSettingsAsync();

        // 1. Sign Out first
        await settingsPage.SignOutAsync();

        // Expected redirect to Login screen
        var loginPage = new LoginPage(CurrentPage);
        await Expect(loginPage.LoginButton).ToBeVisibleAsync(new() { Timeout = 10000 });

        // 2. Log back in via UI
        await loginPage.LoginAsync(_userEmail, ConfigManager.Settings.DefaultPassword);
        
        // Wait until logged in (e.g. TabBar settings tab is visible)
        await Expect(tab.SettingsTab).ToBeVisibleAsync(new() { Timeout = 10000 });

        // 3. Navigate back to Settings and Leave Group
        await tab.NavigateToSettingsAsync();
        await settingsPage.LeaveGroupAsync();

        // Expected redirect to Group Setup chooser (Create/Join buttons visible)
        var groupSetupPage = new GroupSetupPage(CurrentPage);
        await Expect(groupSetupPage.OptionCreateGroup).ToBeVisibleAsync(new() { Timeout = 10000 });
    }
}
