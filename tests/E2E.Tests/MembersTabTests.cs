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
[Category("members")]
public class MembersTabTests : UiBaseTest
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
        var setup = await _dataManager.CreateTestGroupSetupAsync("Members Home", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = _userEmail, Password = password, Name = "Member Leader", Type = "Adult", Resource = 100 }
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
    public async Task TC_E2E_022_ViewMembers_And_NavigateWeeks()
    {
        var tab = new TabBar(CurrentPage);
        var membersPage = new MembersPage(CurrentPage);

        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);
        await tab.NavigateToMembersAsync();

        // 1. Confirm member card exists
        await Expect(CurrentPage.GetByText("Member Leader")).ToBeVisibleAsync();

        // 2. Week label should show "This Week" initially
        await Expect(membersPage.WeekLabel).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("This Week|Current Week|Week", System.Text.RegularExpressions.RegexOptions.IgnoreCase));

        // 3. Navigate back
        await membersPage.NavigateWeekBackAsync();
        await Expect(membersPage.WeekLabel).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("Last Week|Previous Week|Week", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }
}
