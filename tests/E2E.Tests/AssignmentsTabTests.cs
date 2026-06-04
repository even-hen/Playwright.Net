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
[Category("assignments")]
public class AssignmentsTabTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string _groupId = null!;
    private string _userId = null!;
    private string _userEmail = null!;
    private string _taskId = null!;

    [SetUp]
    public async Task SetUpData()
    {
        _dataManager = new SupabaseDataManager();
        _userEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Create standard group with this member
        var setup = await _dataManager.CreateTestGroupSetupAsync("Assignments Household", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = password, Name = "John Householder", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _userId = setup.UserIds[0];

        // Seed a task and today's assignment via API
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
        var taskInput = new PlaywrightFramework.Api.Models.ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Sweep the deck",
            Emoji: "🧹",
            Complexity: 20,
            WeekDays: new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            AvailableFor: new List<string> { "Adult" },
            AssignedTo: _userId,
            Auto: false,
            IsActive: true,
            CreatedBy: _userId,
            CreatedAt: null
        );
        var task = await tasksClient.CreateTaskAsync(taskInput);
        _taskId = task.Id;

        var assignmentsClient = new PlaywrightFramework.Api.Clients.AssignmentsClient(apiContext);
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await assignmentsClient.CreateAssignmentAsync(new PlaywrightFramework.Api.Models.TaskAssignment(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            TaskId: _taskId,
            Title: "Sweep the deck",
            WeekDays: new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            Date: today,
            AssignedTo: _userId,
            Status: "pending",
            Complexity: 20,
            WeekStart: today,
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
    public async Task TC_E2E_011_ViewAssignments_And_MarkAsDone()
    {
        var assignmentsPage = new AssignmentsPage(CurrentPage);
        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);

        // 1. Check card title is visible
        await Expect(CurrentPage.GetByText("Sweep the deck")).ToBeVisibleAsync();

        // 2. Mark task as done
        await assignmentsPage.MarkAssignmentDoneAsync("Sweep the deck");

        // 3. Mark Done button should disappear — task has left the Pending section
        await Expect(CurrentPage.GetByRole(AriaRole.Button, new() { Name = "Mark Done" }))
            .Not.ToBeVisibleAsync(new() { Timeout = 10000 });

        // 4. Task should still be visible on page (now in Completed section)
        await Expect(CurrentPage.GetByText("Sweep the deck")).ToBeVisibleAsync(new() { Timeout = 5000 });

    }
}
