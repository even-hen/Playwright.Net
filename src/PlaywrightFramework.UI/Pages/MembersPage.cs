using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class MembersPage : BasePage
{
    public MembersPage(IPage page) : base(page) { }

    public ILocator WeekLabel => Page.Locator("[data-testid='week-label']").Or(Page.Locator("div").Filter(new() { Has = Page.GetByText("") }).Locator("div").Filter(new() { HasText = "Week" })).First;
    public ILocator PrevWeekButton => Page.Locator("[data-testid='prev-week-btn']").Or(Page.GetByText("")).First;
    public ILocator NextWeekButton => Page.Locator("[data-testid='next-week-btn']").Or(Page.GetByText("")).First;

    // Modal elements
    public ILocator EditMemberModal => Page.Locator("[data-testid='edit-member-modal']").Or(Page.Locator(".member-modal"));
    public ILocator MemberNameInput => Page.Locator("input[placeholder='Name']:visible").Or(Page.Locator("input[placeholder='Full Name']:visible")).Or(Page.Locator("input[name='memberName']:visible")).First;
    public ILocator RoleDropdown => Page.Locator("select[name='memberRole']:visible").Or(Page.GetByRole(AriaRole.Combobox)).First;
    public ILocator SliderHandle => Page.Locator("[data-testid='member-capacity-slider-handle']:visible").Or(Page.Locator("input[type='range']:visible")).First;
    public ILocator SaveButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Save Changes" }).Or(Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" })).First;
    public ILocator RemoveButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Remove from Group" }).Or(Page.GetByRole(AriaRole.Button, new() { Name = "Remove from Group" })).First;

    public async Task NavigateWeekBackAsync() => await PrevWeekButton.ClickAsync();
    public async Task NavigateWeekForwardAsync() => await NextWeekButton.ClickAsync();

    public async Task ClickStatPillAsync(string memberName, string statType)
    {
        var card = Page.Locator("[data-testid='member-card']").Filter(new() { HasText = memberName });
        // statType could be "Done", "Skipped", "Pending"
        var pill = card.Locator($"[data-testid='stat-pill-{statType.ToLower()}']").Or(card.GetByText(statType, new() { Exact = false }));
        await pill.ClickAsync();
    }

    public async Task ClickEditMemberAsync(string memberName)
    {
        var card = Page.Locator("[data-testid='member-card']").Filter(new() { HasText = memberName });
        var editBtn = card.Locator("[data-testid='edit-member-btn']").Or(card.Locator(".edit-icon"));
        await editBtn.ClickAsync();
    }

    public async Task EditMemberDetailsAsync(string name, string roleType, int capacity)
    {
        await MemberNameInput.FillAsync(name);
        
        if (await RoleDropdown.IsVisibleAsync())
        {
            await RoleDropdown.SelectOptionAsync(roleType);
        }

        // Set native range input or slider capacity
        if (await SliderHandle.IsVisibleAsync())
        {
            var type = await SliderHandle.GetAttributeAsync("type");
            if (type == "range")
            {
                await SliderHandle.FillAsync(capacity.ToString());
                await SliderHandle.EvaluateAsync("e => e.dispatchEvent(new Event('change', { bubbles: true }))");
            }
        }

        await SaveButton.ClickAsync();
    }

    public async Task RemoveMemberAsync()
    {
        await RemoveButton.ClickAsync();
        // Handle native prompt dialog if needed, or inline overlay confirmation
        var confirmBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Remove" }).Or(Page.GetByText("Confirm"));
        if (await confirmBtn.IsVisibleAsync())
        {
            await confirmBtn.ClickAsync();
        }
    }
}
