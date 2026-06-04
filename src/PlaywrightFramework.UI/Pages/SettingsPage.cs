using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class SettingsPage : BasePage
{
    public SettingsPage(IPage page) : base(page) { }

    public ILocator ProfileCard => Page.Locator("[data-testid='profile-card']").Or(Page.Locator(".profile-card"));
    public ILocator NotificationTimeDropdown => Page.Locator("xpath=//div[div[text()='Notification Time']]/div[@tabindex='0']").First;
    public ILocator GenerateInviteBtn => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Generate Invite Code" }).Or(Page.Locator("[data-testid='generate-invite-btn']:visible")).First;
    
    public ILocator ThemeLightButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Light" }).Or(Page.Locator("[data-testid='theme-btn-light']:visible")).First;
    public ILocator ThemeDarkButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Dark" }).Or(Page.Locator("[data-testid='theme-btn-dark']:visible")).First;
    
    public ILocator LeaveGroupButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Leave Group" }).Or(Page.Locator("[data-testid='leave-group-btn']:visible")).First;
    public ILocator SignOutButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Sign Out" }).Or(Page.Locator("[data-testid='sign-out-btn']:visible")).First;

    public async Task ChangeNotificationTimeAsync(string hourStr)
    {
        await NotificationTimeDropdown.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await Page.WaitForTimeoutAsync(200);
        await NotificationTimeDropdown.EvaluateAsync("el => el.click()");
        
        var option = Page.GetByText(hourStr, new() { Exact = true }).First;
        await option.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
        await Page.WaitForTimeoutAsync(200);
        await option.EvaluateAsync("el => el.click()");
    }

    public async Task<string> GenerateInviteCodeAsync()
    {
        string code = string.Empty;
        
        EventHandler<IDialog> handler = null!;
        handler = async (_, dialog) =>
        {
            var match = Regex.Match(dialog.Message, @"[A-Z0-9]{8}");
            if (match.Success)
            {
                code = match.Value;
            }
            await dialog.AcceptAsync();
        };

        Page.Dialog += handler;
        try
        {
            await GenerateInviteBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }
        finally
        {
            Page.Dialog -= handler;
        }

        return code;
    }

    public async Task SwitchThemeAsync(string theme)
    {
        if (theme.ToLower() == "dark")
        {
            await ThemeDarkButton.ClickAsync();
        }
        else
        {
            await ThemeLightButton.ClickAsync();
        }
    }

    public async Task LeaveGroupAsync()
    {
        EventHandler<IDialog> handler = async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };
        Page.Dialog += handler;
        try
        {
            await LeaveGroupButton.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
            await Page.WaitForTimeoutAsync(200);
            await LeaveGroupButton.EvaluateAsync("el => el.click()");
            await Page.WaitForTimeoutAsync(500);
        }
        finally
        {
            Page.Dialog -= handler;
        }
    }

    public async Task SignOutAsync()
    {
        EventHandler<IDialog> handler = async (_, dialog) =>
        {
            await dialog.AcceptAsync();
        };
        Page.Dialog += handler;
        try
        {
            await SignOutButton.EvaluateAsync("el => el.scrollIntoView({ block: 'center' })");
            await Page.WaitForTimeoutAsync(200);
            await SignOutButton.EvaluateAsync("el => el.click()");
            
            // Fallback for HTML modal confirm button
            var confirmBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Sign Out" }).Or(Page.GetByText("Confirm")).First;
            if (await confirmBtn.IsVisibleAsync())
            {
                await confirmBtn.EvaluateAsync("el => el.click()");
            }
            
            await Page.WaitForTimeoutAsync(500);
        }
        finally
        {
            Page.Dialog -= handler;
        }
    }
}
