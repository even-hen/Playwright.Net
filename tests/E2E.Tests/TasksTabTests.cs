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
[Category("tasks")]
public class TasksTabTests : UiBaseTest
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
        var setup = await _dataManager.CreateTestGroupSetupAsync("Tasks Home", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = password, Name = "Chore Builder", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _userId = setup.UserIds[0];

        // Seed a default task
        var clientHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {ConfigManager.Settings.SupabaseServiceRoleKey}" }
        };
        var apiContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = clientHeaders
        });
        
        var tasksClient = new PlaywrightFramework.Api.Clients.TasksClient(apiContext);
        await tasksClient.CreateTaskAsync(new PlaywrightFramework.Api.Models.ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Vacuum rooms",
            Emoji: "🧹",
            Complexity: 30,
            WeekDays: new List<int> { 0, 6 },
            AvailableFor: new List<string> { "Adult", "Teen" },
            AssignedTo: _userId,
            Auto: false,
            IsActive: true,
            CreatedBy: _userId,
            CreatedAt: null
        ));

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
    public async Task TC_E2E_016_ViewTaskList_And_Search()
    {
        var tab = new TabBar(CurrentPage);
        var tasksPage = new TasksPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToTasksAsync();

        // 1. Verify card exists
        await Expect(CurrentPage.GetByText("Vacuum rooms")).ToBeVisibleAsync();

        // 2. Search for non-matching query
        await tasksPage.SearchAsync("dishes");
        await Expect(CurrentPage.GetByText("Vacuum rooms")).Not.ToBeVisibleAsync();

        // 3. Clear search and check task is visible again
        await tasksPage.ClearSearchAsync();
        await Expect(CurrentPage.GetByText("Vacuum rooms")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TC_E2E_017_CreateNewTask_ViaModal()
    {
        var tab = new TabBar(CurrentPage);
        var tasksPage = new TasksPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToTasksAsync();

        // Open modal
        await tasksPage.OpenAddTaskModalAsync();

        // Fill and save
        await tasksPage.FillTaskFormAndSaveAsync(
            "Wash the dishes",
            20,
            new List<string> { "Mon", "Wed", "Fri" },
            new List<string> { "Adult", "Teen" },
            "🌱"
        );

        // Verify task appears in list
        await Expect(CurrentPage.GetByText("Wash the dishes")).ToBeVisibleAsync();
    }
}
