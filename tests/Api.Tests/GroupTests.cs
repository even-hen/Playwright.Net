using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.Groups;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("groups")]
public class GroupTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _userId = null!;
    private static string _userToken = null!;
    private static readonly System.Collections.Concurrent.ConcurrentBag<string> _createdGroupIds = new();
    
    private GroupsClient _groupsClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var email = $"user_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        _userId = await _dataManager.CreateAuthUserAdminAsync(email, password, "Group Test User");
        await _dataManager.CreateUserProfileAsync(_userId, email, "Group Test User", "Adult", 100, null);

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
        var authResponse = await authClient.SignInAsync(email, password);
        _userToken = authResponse.AccessToken;
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDownCleanup()
    {
        foreach (var groupId in _createdGroupIds)
        {
            await _dataManager.DeleteGroupCascadeAsync(groupId);
        }

        if (!string.IsNullOrEmpty(_userId))
        {
            // Just clean up the user if group wasn't created
            var db = new SupabaseDataManager();
            await db.DeleteGroupCascadeAsync(_userId);
        }
    }

    [SetUp]
    public async Task SetUpClientContext()
    {
        var headers = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_userToken}" }
        };
        var context = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = headers
        });
        _groupsClient = new GroupsClient(context);
    }

    [Test]
    [Category("critical")]
    public async Task TC_GROUP_001_CreateGroupWithValidName()
    {
        var groupName = "The New Family Unit";
        var group = await _groupsClient.CreateGroupAsync(groupName, _userId);
        _createdGroupIds.Add(group.Id);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(group, Is.Not.Null);
            Assert.That(group.Name, Is.EqualTo(groupName));
            Assert.That(group.CreatedBy, Is.EqualTo(_userId));
            Assert.That(group.AutoDistribution, Is.True);
        }));

        // Verify the creator profile's group_id was updated to this group ID
        var usersClient = new UsersClient(ApiRequestContext);
        var profile = await usersClient.GetUserProfileSingleAsync(_userId);
        Assert.That(profile.GroupId, Is.EqualTo(group.Id));
    }

    [Test]
    public async Task TC_GROUP_010_FetchGroupSettings()
    {
        // Setup a dummy group
        var dummyGroupName = "Settings Group";
        var group = await _groupsClient.CreateGroupAsync(dummyGroupName, _userId);
        _createdGroupIds.Add(group.Id);

        var fetchedGroup = await _groupsClient.GetGroupSingleAsync(group.Id);
        
        Assert.Multiple((Action)(() =>
        {
            Assert.That(fetchedGroup.Id, Is.EqualTo(group.Id));
            Assert.That(fetchedGroup.AutoDistribution, Is.True);
        }));
    }

    [Test]
    public async Task TC_GROUP_020_ToggleAutoDistribution()
    {
        var groupName = "Toggle Group";
        var group = await _groupsClient.CreateGroupAsync(groupName, _userId);
        _createdGroupIds.Add(group.Id);

        // Toggle to false
        await _groupsClient.UpdateGroupSettingsAsync(group.Id, new { auto_distribution = false });

        var fetched = await _groupsClient.GetGroupSingleAsync(group.Id);
        Assert.That(fetched.AutoDistribution, Is.False);

        // Toggle back to true
        await _groupsClient.UpdateGroupSettingsAsync(group.Id, new { auto_distribution = true });

        fetched = await _groupsClient.GetGroupSingleAsync(group.Id);
        Assert.That(fetched.AutoDistribution, Is.True);
    }
}
