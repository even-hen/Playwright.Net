using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Assignments;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("assignments")]
public class AssignmentTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _groupId = null!;
    private static string _adultId = null!;
    private static string _adultToken = null!;
    private static string _taskId = null!;
    
    private AssignmentsClient _assignmentsClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@test.com";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seed a family group
        var setup = await _dataManager.CreateTestGroupSetupAsync("Assignment Test Smiths", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = adultEmail, Password = password, Name = "John Assigner", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _adultId = setup.UserIds[0];

        // Retrieve tokens
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var authHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Content-Type", "application/json" }
        };
        var tempAuthContext = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = authHeaders
        });
        var authClient = new AuthClient(tempAuthContext);
        var adultAuth = await authClient.SignInAsync(adultEmail, password);
        _adultToken = adultAuth.AccessToken;

        // Seed a task for creating assignments
        var headers = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_adultToken}" }
        };
        var context = await playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = headers
        });
        var tasksClient = new TasksClient(context);

        var taskInput = new ChoreTask(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            Title: "Sweep kitchen",
            Emoji: "🧹",
            Complexity: 10,
            WeekDays: new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            AvailableFor: new List<string> { "Adult" },
            AssignedTo: _adultId,
            Auto: false,
            IsActive: true,
            CreatedBy: _adultId,
            CreatedAt: null
        );
        var task = await tasksClient.CreateTaskAsync(taskInput);
        _taskId = task.Id;
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
        _assignmentsClient = new AssignmentsClient(context);
    }

    [Test]
    [Category("critical")]
    public async Task TC_ASSIGN_001_CreateAndQueryAssignment()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var assignmentInput = new TaskAssignment(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            TaskId: _taskId,
            Title: "Sweep kitchen",
            WeekDays: new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            Date: today,
            AssignedTo: _adultId,
            Status: "pending",
            Complexity: 10,
            WeekStart: today, // Simple placeholder for week start
            CreatedAt: null
        );

        var created = await _assignmentsClient.CreateAssignmentAsync(assignmentInput);
        
        Assert.Multiple((Action)(() =>
        {
            Assert.That(created.GroupId, Is.EqualTo(_groupId));
            Assert.That(created.TaskId, Is.EqualTo(_taskId));
            Assert.That(created.AssignedTo, Is.EqualTo(_adultId));
            Assert.That(created.Status, Is.EqualTo("pending"));
        }));

        // Query assignments
        var list = await _assignmentsClient.GetAssignmentsAsync(_groupId, today);
        Assert.That(list.Exists(a => a.Id == created.Id), Is.True);
    }

    [Test]
    public async Task TC_ASSIGN_010_MarkAssignmentAsDone()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var assignmentInput = new TaskAssignment(
            Id: Guid.NewGuid().ToString(),
            GroupId: _groupId,
            TaskId: _taskId,
            Title: "Sweep kitchen",
            WeekDays: new List<int> { 0, 1, 2, 3, 4, 5, 6 },
            Date: today,
            AssignedTo: _adultId,
            Status: "pending",
            Complexity: 10,
            WeekStart: today,
            CreatedAt: null
        );

        var created = await _assignmentsClient.CreateAssignmentAsync(assignmentInput);

        // Update status to done
        await _assignmentsClient.UpdateAssignmentStatusAsync(created.Id, "done");

        var list = await _assignmentsClient.GetAssignmentsAsync(_groupId, today);
        var updated = list.Find(a => a.Id == created.Id);
        
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Status, Is.EqualTo("done"));
    }
}
