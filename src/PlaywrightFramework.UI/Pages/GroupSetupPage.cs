using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class GroupSetupPage : BasePage
{
    public GroupSetupPage(IPage page) : base(page) { }

    public ILocator OptionCreateGroup => Page.GetByText("Create a Group", new() { Exact = true }).Locator("visible=true").First;
    public ILocator OptionJoinGroup => Page.GetByText("Join a Group", new() { Exact = true }).Locator("visible=true").First;
    
    public ILocator GroupNameInput => Page.Locator("input[placeholder='e.g. The Smiths']:visible").Or(Page.Locator("input[placeholder='Group Name']:visible")).First;
    public ILocator InviteCodeInput => Page.Locator("input[placeholder='Paste your invite code']:visible").Or(Page.Locator("input[placeholder='Invite Code']:visible")).First;
    
    public ILocator CreateGroupBtn => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Create Group" })
        .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Create Group" }))
        .First;
    public ILocator JoinGroupBtn => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Join Group" })
        .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Join Group" }))
        .First;
    
    public ILocator BackButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Back" })
        .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Back" }))
        .Or(Page.GetByRole(AriaRole.Link, new() { Name = "Back" }))
        .First;

    public async Task CreateGroupAsync(string name)
    {
        await OptionCreateGroup.ClickAsync();
        await GroupNameInput.FillAsync(name);
        await CreateGroupBtn.ClickAsync();
    }

    public async Task JoinGroupAsync(string inviteCode)
    {
        await OptionJoinGroup.ClickAsync();
        await InviteCodeInput.FillAsync(inviteCode);
        await JoinGroupBtn.ClickAsync();
    }

    public async Task GoBackAsync()
    {
        await BackButton.ClickAsync();
    }
}
