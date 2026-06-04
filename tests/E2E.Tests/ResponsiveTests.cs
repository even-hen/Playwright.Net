using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.UI.Pages;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace E2E.Tests.Responsive;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("e2e")]
[Category("responsive")]
public class ResponsiveTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string _groupId = null!;
    private string _userId = null!;
    private string _userEmail = null!;
    private string _password = null!;

    [SetUp]
    public async Task SetUpData()
    {
        _dataManager = new SupabaseDataManager();
        _userEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        _password = ConfigManager.Settings.DefaultPassword;

        // Create standard group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Responsive House", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = _password, Name = "Papa Viewport", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _userId = setup.UserIds[0];
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
    public async Task TC_E2E_040_MobileViewport_ResponsiveLayout()
    {
        var statePath = await AuthFixture.GetStorageStatePathAsync(_userEmail, _password);
        
        // 1. Setup mobile viewport
        await SetupCustomContextAsync(new()
        {
            StorageStatePath = statePath,
            ViewportSize = new() { Width = 375, Height = 812 }
        });

        var tab = new TabBar(CurrentPage);
        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);

        // Verify elements on mobile viewport are visible
        await Expect(tab.ToDoTab).ToBeVisibleAsync();
        await tab.NavigateToTasksAsync();
        
        var tasksPage = new TasksPage(CurrentPage);
        // Verify we can see tasks content on mobile
        await Expect(tasksPage.SearchInput).ToBeVisibleAsync();
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_041_DesktopViewport_FullLayout()
    {
        var statePath = await AuthFixture.GetStorageStatePathAsync(_userEmail, _password);

        // 2. Setup desktop viewport
        await SetupCustomContextAsync(new()
        {
            StorageStatePath = statePath,
            ViewportSize = new() { Width = 1280, Height = 720 }
        });

        var tab = new TabBar(CurrentPage);
        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);

        // Verify elements on desktop layout
        await Expect(tab.ToDoTab).ToBeVisibleAsync();
        await tab.NavigateToMembersAsync();

        var membersPage = new MembersPage(CurrentPage);
        // Verify members page is accessible and week label is visible
        await Expect(membersPage.WeekLabel).ToBeVisibleAsync();
    }
}
