using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class AssignmentsPage : BasePage
{
    public AssignmentsPage(IPage page) : base(page) { }

    public ILocator ToggleMine => Page.Locator("[data-testid='toggle-mine']").Or(Page.GetByText("Mine", new() { Exact = true }));
    public ILocator ToggleAll => Page.Locator("[data-testid='toggle-all']").Or(Page.GetByText("All", new() { Exact = true }));
    
    public ILocator PendingSection   => Page.Locator("[data-testid='section-pending']").Or(Page.Locator(".section-pending")).Or(Page.Locator("div, section").Filter(new() { HasText = "PENDING" }).Or(Page.Locator("div, section").Filter(new() { HasText = "Pending" })).First);
    public ILocator CompletedSection => Page.Locator("[data-testid='section-completed']").Or(Page.Locator(".section-completed")).Or(Page.Locator("div, section").Filter(new() { HasText = "COMPLETED" }).Or(Page.Locator("div, section").Filter(new() { HasText = "Completed" })).First);
    public ILocator SkippedSection   => Page.Locator("[data-testid='section-skipped']").Or(Page.Locator(".section-skipped")).Or(Page.Locator("div, section").Filter(new() { HasText = "SKIPPED" }).Or(Page.Locator("div, section").Filter(new() { HasText = "Skipped" })).First);

    
    public ILocator EmptyStateText => Page.GetByText("All clear", new() { Exact = false }).Or(Page.Locator("[data-testid='empty-state']"));

    public async Task FilterByMineAsync() => await ToggleMine.ClickAsync();
    public async Task FilterByAllAsync() => await ToggleAll.ClickAsync();

    public async Task MarkAssignmentDoneAsync(string taskTitle)
    {
        // Find the assignment row/card containing the title, then click its "Mark Done" button
        var card = Page.Locator("[data-testid='assignment-card']").Filter(new() { HasText = taskTitle })
            .Or(Page.Locator(".assignment-card").Filter(new() { HasText = taskTitle }))
            .Or(Page.Locator("li, div, article").Filter(new() { HasText = taskTitle }));

        // Try finding the button inside the card first, then fall back to a page-level search near the title
        var markDoneButton = card.GetByRole(AriaRole.Button, new() { Name = "Mark Done" })
            .Or(card.GetByText("Mark Done", new() { Exact = true }));

        if (await markDoneButton.IsVisibleAsync())
        {
            await markDoneButton.ClickAsync();
        }
        else
        {
            // Fallback: find "Mark Done" button anywhere on the page visible near the task title
            await Page.GetByRole(AriaRole.Button, new() { Name = "Mark Done" }).First.ClickAsync();
        }
    }

}
