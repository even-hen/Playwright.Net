using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Api.Models;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace Api.Tests.InviteLinks;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("api")]
[Category("invites")]
public class InviteTests : ApiBaseTest
{
    private static SupabaseDataManager _dataManager = null!;
    private static string _groupId = null!;
    private static string _adultId = null!;
    private static string _adultToken = null!;
    private static string _joiningUserId = null!;
    private static string _joiningUserToken = null!;
    
    private InviteLinksClient _inviteClient = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUpData()
    {
        _dataManager = new SupabaseDataManager();

        var adultEmail = $"adult_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var joiningEmail = $"join_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        // Seed group for inviter
        var setup = await _dataManager.CreateTestGroupSetupAsync("Invite Test Smiths", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = adultEmail, Password = password, Name = "John Inviter", Type = "Adult", Resource = 100 }
        });
        _groupId = setup.GroupId;
        _adultId = setup.UserIds[0];

        // Seed a user who has no group and wants to join
        _joiningUserId = await _dataManager.CreateAuthUserAdminAsync(joiningEmail, password, "Sam Joiner");
        await _dataManager.CreateUserProfileAsync(_joiningUserId, joiningEmail, "Sam Joiner", "Child", 30, null);

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

        var joiningAuth = await authClient.SignInAsync(joiningEmail, password);
        _joiningUserToken = joiningAuth.AccessToken;
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDownCleanup()
    {
        if (!string.IsNullOrEmpty(_groupId))
        {
            await _dataManager.DeleteGroupCascadeAsync(_groupId);
        }
        if (!string.IsNullOrEmpty(_joiningUserId))
        {
            try
            {
                var db = new SupabaseDataManager();
                await db.DeleteGroupCascadeAsync(_joiningUserId);
            }
            catch { }
        }
    }

    [SetUp]
    public async Task SetUpClientContexts()
    {
        var adultHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_adultToken}" }
        };
        var adultContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = adultHeaders
        });
        _inviteClient = new InviteLinksClient(adultContext);
    }

    [Test]
    [Category("critical")]
    public async Task TC_INVITE_001_GenerateValidInviteCode()
    {
        var token = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var invite = await _inviteClient.CreateInviteLinkAsync(token, _groupId, expiresAt);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(invite.Token, Is.EqualTo(token));
            Assert.That(invite.GroupId, Is.EqualTo(_groupId));
            Assert.That(invite.UsedBy, Is.Empty);
            // Allow small delta for timestamp parsing
            Assert.That((invite.ExpiresAt.ToUniversalTime() - expiresAt).Duration(), Is.LessThan(TimeSpan.FromSeconds(5)));
        }));
    }

    [Test]
    [Category("critical")]
    public async Task TC_INVITE_010_JoinGroupWithValidCode()
    {
        var token = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        var expiresAt = DateTime.UtcNow.AddHours(2);

        // Creator generates the link
        var invite = await _inviteClient.CreateInviteLinkAsync(token, _groupId, expiresAt);

        // Joining user context
        var joiningHeaders = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Authorization", $"Bearer {_joiningUserToken}" }
        };
        var joiningContext = await Playwright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = joiningHeaders
        });
        var joiningInviteClient = new InviteLinksClient(joiningContext);
        var joiningUsersClient = new UsersClient(joiningContext);

        // 1. Resolve token
        var resolved = await joiningInviteClient.GetInviteLinkSingleAsync(token);
        Assert.That(resolved.GroupId, Is.EqualTo(_groupId));

        // 2. Perform Join: Update user profile's group ID
        await joiningUsersClient.UpdateUserProfileAsync(_joiningUserId, new { group_id = _groupId });

        // 3. Mark token as used
        var updatedUsedBy = new List<string>(resolved.UsedBy) { _joiningUserId };
        await joiningInviteClient.UpdateInviteLinkUsageAsync(token, updatedUsedBy);

        // Verify state
        var userProfile = await joiningUsersClient.GetUserProfileSingleAsync(_joiningUserId);
        Assert.That(userProfile.GroupId, Is.EqualTo(_groupId));

        var finalInviteState = await joiningInviteClient.GetInviteLinkSingleAsync(token);
        Assert.That(finalInviteState.UsedBy, Contains.Item(_joiningUserId));
    }
}
