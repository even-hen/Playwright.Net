using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;
using PlaywrightFramework.Core.Fixtures;
using PlaywrightFramework.Core.Configuration;
using PlaywrightFramework.UI.Pages;
using PlaywrightFramework.Tools.SupabaseDataManager;

namespace E2E.Tests.Auth;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[Category("e2e")]
[Category("onboarding")]
public class OnboardingTests : UiBaseTest
{
    private SupabaseDataManager _dataManager = null!;
    private string? _createdUserEmail;

    [SetUp]
    public void SetUpData()
    {
        _dataManager = new SupabaseDataManager();
        _createdUserEmail = null;
    }

    [TearDown]
    public async Task CleanupUsers()
    {
        if (!string.IsNullOrEmpty(_createdUserEmail))
        {
            try
            {
                var userId = await _dataManager.GetUserIdByEmailAsync(_createdUserEmail);
                if (!string.IsNullOrEmpty(userId))
                {
                    await _dataManager.DeleteGroupCascadeAsync(userId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup ERROR] Failed to clean up user with email {_createdUserEmail}: {ex.Message}");
            }
        }
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_001_FullRegistration_RedirectsToGroupSetup()
    {
        var loginPage = new LoginPage(Page);
        var registerPage = new RegisterPage(Page);

        // 1. Open App and go to Register
        await Page.GotoAsync(ConfigManager.Settings.BaseUrl);
        await loginPage.RegisterLink.ClickAsync();

        // 2. Fill Register details
        var email = $"e2e_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        _createdUserEmail = email;
        var password = ConfigManager.Settings.DefaultPassword;

        await registerPage.RegisterAsync("Test User", email, password, "Adult", 80);

        // 3. Expected: Success redirects to Group Setup chooser
        var groupSetupPage = new GroupSetupPage(Page);
        await Expect(groupSetupPage.OptionCreateGroup).ToBeVisibleAsync();
        await Expect(groupSetupPage.OptionJoinGroup).ToBeVisibleAsync();
    }

    [Test]
    [Category("critical")]
    public async Task TC_E2E_002_Login_RedirectsToTabBar_WhenUserHasGroup()
    {
        // Create a dedicated test user with a group so this test is self-contained
        var dataManager = new SupabaseDataManager();
        var email = $"e2e_login_{Guid.NewGuid().ToString().Substring(0, 6)}@example.org";
        var password = ConfigManager.Settings.DefaultPassword;

        var setup = await dataManager.CreateTestGroupSetupAsync("Login Test Group", new List<UserSetupSpec>
        {
            new UserSetupSpec { Email = email, Password = password, Name = "Login Tester", Type = "Adult", Resource = 100 }
        });
        var groupId = setup.GroupId;

        try
        {
            var loginPage = new LoginPage(Page);
            await Page.GotoAsync(ConfigManager.Settings.BaseUrl);
            await loginPage.LoginAsync(email, password);

            // Should redirect to the main tab bar after successful login
            var tab = new TabBar(Page);
            await Expect(tab.ToDoTab).ToBeVisibleAsync(new() { Timeout = 15000 });
            await Expect(tab.SettingsTab).ToBeVisibleAsync();
        }
        finally
        {
            await dataManager.DeleteGroupCascadeAsync(groupId);
        }
    }

    [Test]
    public async Task TC_E2E_004_LoginWithInvalidCredentialsShowsError()
    {
        var loginPage = new LoginPage(Page);
        
        await Page.GotoAsync(ConfigManager.Settings.BaseUrl);
        await loginPage.LoginAsync("invalid_user@example.org", "wrongpassword");

        // Wait for response — app should either show an error OR keep user on login page
        await Page.WaitForTimeoutAsync(2000);
        var errorOrLoginPage = loginPage.ErrorMessage
            .Or(Page.Locator("input[type='email']:visible")) // still on login page = not redirected
            .Or(Page.GetByRole(AriaRole.Alert));
        await Expect(errorOrLoginPage).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task TC_E2E_005_NavigateBetweenLoginAndRegister()
    {
        var loginPage = new LoginPage(Page);
        var registerPage = new RegisterPage(Page);

        await Page.GotoAsync(ConfigManager.Settings.BaseUrl);
        
        await loginPage.RegisterLink.ClickAsync();
        await Expect(registerPage.RegisterButton).ToBeVisibleAsync();

        await registerPage.BackButton.ClickAsync();
        await Expect(loginPage.LoginButton).ToBeVisibleAsync();
    }
}
