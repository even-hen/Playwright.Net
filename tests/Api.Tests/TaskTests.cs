using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Tasks;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("tasks")]
public class TaskTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _groupId = null!;
    private static string _adultId = null!;
    private static string _adultToken = null!;
    
    private TasksClient _tasksClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seed a family group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Task Test Smiths", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = adultEmail, Password = password, Name = "John Builder", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _adultId = setup.UserIds[0];

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
        _tasksClient = new TasksClient(context);
    }

    [Test]
    [Category("critical")]
    public async Task TC_TASK_001_CreateTaskWithAllValidFields()
    {
        var taskInput = new ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Water plants",
            Emoji: "🌱",
            Complexity: 20,
            WeekDays: new List<int> { 1, 3, 5 },
            AvailableFor: new List<string> { "Adult", "Teen", "Child" },
            AssignedTo: null,
            Auto: true,
            IsActive: true,
            CreatedBy: _adultId,
            CreatedAt: null
        );

        var task = await _tasksClient.CreateTaskAsync(taskInput);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(task.Title, Is.EqualTo("Water plants"));
            Assert.That(task.Emoji, Is.EqualTo("🌱"));
            Assert.That(task.Complexity, Is.EqualTo(20));
            Assert.That(task.WeekDays, Is.EquivalentTo(new List<int> { 1, 3, 5 }));
            Assert.That(task.AvailableFor, Is.EquivalentTo(new List<string> { "Adult", "Teen", "Child" }));
            Assert.That(task.Auto, Is.True);
            Assert.That(task.IsActive, Is.True);
        }));
    }

    [Test]
    public async Task TC_TASK_020_UpdateTaskTitle()
    {
        var taskInput = new ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Sweep patio",
            Emoji: "🧹",
            Complexity: 10,
            WeekDays: new List<int> { 2, 4 },
            AvailableFor: new List<string> { "Adult", "Teen" },
            AssignedTo: _adultId,
            Auto: false,
            IsActive: true,
            CreatedBy: _adultId,
            CreatedAt: null
        );

        var task = await _tasksClient.CreateTaskAsync(taskInput);
        
        // Update Title
        var newTitle = "Sweep entire patio";
        await _tasksClient.UpdateTaskAsync(task.Id, new { title = newTitle });

        var fetchedTasks = await _tasksClient.GetTasksAsync(_groupId);
        var updatedTask = fetchedTasks.Find(t => t.Id == task.Id);
        
        Assert.That(updatedTask, Is.Not.Null);
        Assert.That(updatedTask!.Title, Is.EqualTo(newTitle));
    }

    [Test]
    public async Task TC_TASK_022_DeactivateTask()
    {
        var taskInput = new ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Wash windows",
            Emoji: "🧼",
            Complexity: 30,
            WeekDays: new List<int> { 6 },
            AvailableFor: new List<string> { "Adult" },
            AssignedTo: null,
            Auto: true,
            IsActive: true,
            CreatedBy: _adultId,
            CreatedAt: null
        );

        var task = await _tasksClient.CreateTaskAsync(taskInput);

        // Deactivate Task
        await _tasksClient.UpdateTaskAsync(task.Id, new { is_active = false });

        var fetchedTasks = await _tasksClient.GetTasksAsync(_groupId);
        var updatedTask = fetchedTasks.Find(t => t.Id == task.Id);
        
        Assert.That(updatedTask, Is.Not.Null);
        Assert.That(updatedTask!.IsActive, Is.False);
    }

    [Test]
    [Category("critical")]
    public async Task TC_TASK_040_DeleteTaskSuccessfully()
    {
        var taskInput = new ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Take out recycling",
            Emoji: "♻️",
            Complexity: 15,
            WeekDays: new List<int> { 3 },
            AvailableFor: new List<string> { "Adult", "Teen", "Child" },
            AssignedTo: null,
            Auto: true,
            IsActive: true,
            CreatedBy: _adultId,
            CreatedAt: null
        );

        var task = await _tasksClient.CreateTaskAsync(taskInput);
        
        // Verify task exists
        var listBefore = await _tasksClient.GetTasksAsync(_groupId);
        Assert.That(listBefore.Exists(t => t.Id == task.Id), Is.True);

        // Delete
        await _tasksClient.DeleteTaskAsync(task.Id);

        // Verify task is deleted
        var listAfter = await _tasksClient.GetTasksAsync(_groupId);
        Assert.That(listAfter.Exists(t => t.Id == task.Id), Is.False);
    }
}
