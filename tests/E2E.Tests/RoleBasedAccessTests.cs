using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Playwright;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.UI.Pages;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace E2E.Tests.RBAC;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("e2e")]
[Category("rbac")]
public class RoleBasedAccessTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string _groupId = null!;
    
    private string _adultEmail = null!;
    private string _teenEmail = null!;
    private string _password = null!;

    [SetUp]
    public async Task SetUpRolesData()
    {
        _dataManager = new SupabaseDataManager();
        _adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        _teenEmail = $"teen_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        _password = ConfigManager.Settings.DefaultPassword;

        // Create group with Adult and Teen members
        var setup = await _dataManager.CreateTestGroupSetupAsync("RBAC House", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _adultEmail, Password = _password, Name = "Parent Smith", Type = "Adult", Resource = 100 },
            new UserSetupSpec { Email = _teenEmail, Password = _password, Name = "Teen Smith", Type = "Teen", Resource = 60 }
        });
        _groupId = setup.GroupId;
    }

    [TearDown]
    public async Task CleanupRolesData()
    {
        if (!string.IsNullOrEmpty(_groupId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_groupId);
        }
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_037_AdultSeesAllAdminUIElements()
    {
        var statePath = await AuthFixture.GetStorageStatePathAsync(_adultEmail, _password);
        await SetupCustomContextAsync(new() { StorageStatePath = statePath });

        var tab = new TabBar(CurrentPage);
        var tasksPage = new TasksPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToTasksAsync();

        // Adult should see the Add Task button
        await Expect(tasksPage.AddTaskButton).ToBeVisibleAsync();
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_036_TeenSeesRestrictedUIElements()
    {
        var statePath = await AuthFixture.GetStorageStatePathAsync(_teenEmail, _password);
        await SetupCustomContextAsync(new() { StorageStatePath = statePath });

        var tab = new TabBar(CurrentPage);
        var tasksPage = new TasksPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToTasksAsync();

        // Teen should NOT see the Add Task button
        await Expect(tasksPage.AddTaskButton).Not.ToBeVisibleAsync();
    }
}
