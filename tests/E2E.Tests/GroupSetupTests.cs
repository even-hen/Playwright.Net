using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.UI.Pages;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace E2E.Tests.Groups;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("e2e")]
[Category("groups")]
public class GroupSetupTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string _userId = null!;
    private string _userEmail = null!;
    private string? _createdGroupId;

    [SetUp]
    public async Task SetUpUserWithoutGroup()
    {
        _dataManager = new SupabaseDataManager();
        _userEmail = $"nogroup_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seed auth user who doesn't belong to any group
        _userId = await _dataManager.CreateAuthUserAdminAsync(_userEmail, password, "No Group User");
        await _dataManager.CreateUserProfileAsync(_userId, _userEmail, "No Group User", "Adult", 100, null);

        // Pre-save storage state so test starts logged in
        var statePath = await AuthFixture.GetStorageStatePathAsync(_userEmail, password);
        
        // Re-create page context using storage state
        await SetupCustomContextAsync(new()
        {
            StorageStatePath = statePath
        });
        
        _createdGroupId = null;
    }

    [TearDown]
    public async Task CleanupGroup()
    {
        if (!string.IsNullOrEmpty(_createdGroupId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_createdGroupId);
        }
        else if (!string.IsNullOrEmpty(_userId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_userId);
        }
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_007_CreateGroup_RedirectsToAssignments()
    {
        var groupSetupPage = new GroupSetupPage(CurrentPage);
        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);

        // Fill group name and save
        await groupSetupPage.CreateGroupAsync("The Test Smiths");

        // Verify redirect to Assignments/To Do screen
        var tab = new TabBar(CurrentPage);
        await Expect(tab.ToDoTab).ToBeVisibleAsync();

        // Get group ID via API to verify redirect created the group
        var clientHeaders = new System.Collections.Generic.Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {ConfigManager.Settings.SupabaseServiceRoleKey}" }
        };
        var usersClient = new PlaywrightFramework.Api.Clients.UsersClient(
            await Playwright.APIRequest.NewContextAsync(new()
            {
                BaseURL = ConfigManager.Settings.ApiBaseUrl,
                ExtraHTTPHeaders = clientHeaders
            })
        );
        var profile = await usersClient.GetUserProfileSingleAsync(_userId);
        _createdGroupId = profile.GroupId;

        Assert.That(_createdGroupId, Is.Not.Null.Or.Empty);

    }

    [Test]
    public async Task TC_E2E_009_JoinGroupWithInvalidCodeShowsError()
    {
        var groupSetupPage = new GroupSetupPage(CurrentPage);
        await CurrentPage.GotoAsync(ConfigManager.Settings.BaseUrl);

        // Try to join with wrong token
        await groupSetupPage.JoinGroupAsync("INVALIDX");

        // After clicking Join, the app should either show an error or keep user on the join page.
        // The app currently stays on the Join Group page without a redirect — verify that.
        await CurrentPage.WaitForTimeoutAsync(2000); // allow any async response

        // Check: user gets the exact invalid invite code error message
        await Expect(CurrentPage.GetByText("Invalid invite code"))
            .ToBeVisibleAsync(new() { Timeout = 10000 });

    }
}
