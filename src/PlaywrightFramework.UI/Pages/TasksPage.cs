using Microsoft.Playwright;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class TasksPage : BasePage
{
    public TasksPage(IPage page) : base(page) { }

    public ILocator AddTaskButton => Page.Locator("[data-testid='add-task-btn']:visible").Or(Page.GetByText("+ Add Task", new() { Exact = true }).Locator("visible=true")).Or(Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Add Task" })).First;
    public ILocator SearchInput => Page.Locator("input[type='search']:visible").Or(Page.GetByPlaceholder("search", new() { Exact = false })).First;
    public ILocator SearchClearButton => Page.Locator("[data-testid='clear-search']:visible")
        .Or(Page.Locator("button[aria-label*='clear' i]:visible"))
        .Or(Page.Locator("button[aria-label*='close' i]:visible"))
        .Or(Page.Locator("input[type='search'] ~ button:visible"))
        .Or(Page.Locator("input ~ button:visible").Filter(new() { HasText = "" })).First;
    public ILocator ShuffleButton => Page.Locator("[data-testid='shuffle-btn']:visible").Or(Page.GetByText("🔀")).First;

    // Modal elements
    public ILocator ModalContainer => Page.Locator("[data-testid='task-modal']").Or(Page.Locator(".task-modal"));
    public ILocator TaskTitleInput => Page.Locator("input[placeholder='e.g. Wash dishes']:visible").Or(Page.GetByPlaceholder("e.g. Wash dishes")).Or(Page.Locator("input[name='title']:visible")).First;
    public ILocator ComplexityInput => Page.Locator("input[placeholder='10']:visible").Or(Page.Locator("input[value='10']:visible")).Or(Page.Locator("input[name='complexity']:visible")).First;
    public ILocator ActiveSwitch => Page.Locator("[data-testid='task-active-switch']:visible").Or(Page.Locator("[role='switch']:visible")).Or(Page.Locator("input[type='checkbox']:visible")).First;
    public ILocator SaveTaskButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Save Task" })
        .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Save Task" }))
        .Or(Page.GetByText("Save Task", new() { Exact = true }))
        .First;
    public ILocator DeleteTaskButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Delete Task" }).First;

    public async Task SearchAsync(string query)
    {
        await SearchInput.FillAsync(query);
    }

    public async Task ClearSearchAsync()
    {
        // Try the visual X button first (short timeout to not block on missing element)
        var clearBtn = Page.Locator("[data-testid='clear-search']")
            .Or(Page.Locator("button[aria-label*='clear' i]"))
            .Or(Page.Locator("button[aria-label*='close' i]"));

        try
        {
            await clearBtn.ClickAsync(new() { Timeout = 2000 });
        }
        catch
        {
            // Clear button not found — directly clear the input value and dispatch events
            await SearchInput.FillAsync("");
            await SearchInput.DispatchEventAsync("input");
            await SearchInput.DispatchEventAsync("change");
        }
    }

    public async Task OpenAddTaskModalAsync()
    {
        await AddTaskButton.ClickAsync();
    }

    public async Task FillTaskFormAndSaveAsync(string title, int complexity, List<string> weekdays, List<string> roles, string emoji)
    {
        var modal = Page.Locator("[role='dialog']:visible").Or(ModalContainer).First;
        await TaskTitleInput.FillAsync(title);
        await ComplexityInput.FillAsync(complexity.ToString());
        
        // Select weekdays: day chips are toggles — click those NOT yet in the desired set to deselect,
        // and click those in the desired set that are not yet selected.
        // Simplest approach: press each desired day chip; if the app toggles on click this ensures selection.
        // First deselect all days by clicking each that is active, then select desired ones.
        var allDayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        foreach (var day in allDayNames)
        {
            var chip = modal.GetByText(day, new() { Exact = true }).First;
            var isSelected = await chip.EvaluateAsync<bool>("el => el.classList.contains('active') || el.getAttribute('aria-pressed') === 'true' || el.getAttribute('data-selected') === 'true' || getComputedStyle(el).color !== 'rgb(0, 0, 0)'");
            bool shouldBeSelected = weekdays.Contains(day);
            if (isSelected != shouldBeSelected)
                await chip.ClickAsync();
        }

        // Select available roles: same toggle approach — only click chips that need to change state
        var allRoleNames = new[] { "Adult", "Teen", "Child" };
        foreach (var role in allRoleNames)
        {
            var chip = modal.GetByText(role, new() { Exact = true }).First;
            // Detect selected state via background colour: selected chips have a non-white background
            var isSelected = await chip.EvaluateAsync<bool>(@"el => {
                const bg = window.getComputedStyle(el).backgroundColor;
                const rgb = bg.match(/\d+/g);
                if (!rgb) return false;
                const [r, g, b] = rgb.map(Number);
                return !(r > 220 && g > 220 && b > 220);
            }");
            bool shouldBeSelected = roles.Contains(role);
            if (isSelected != shouldBeSelected)
                await chip.ClickAsync();
        }

        // Select emoji
        var emojiElement = modal.GetByText(emoji).Or(modal.Locator($"[data-testid='emoji-{emoji}']")).First;
        if (await emojiElement.IsVisibleAsync())
        {
            await emojiElement.ClickAsync();
        }

        await SaveTaskButton.ClickAsync();
        // Wait for the modal to close after saving
        await Page.WaitForSelectorAsync("[role='dialog']", new() { State = WaitForSelectorState.Hidden, Timeout = 10000 });
    }

    public async Task ClickEditTaskAsync(string taskTitle)
    {
        var card = Page.Locator("[data-testid='task-card']").Filter(new() { HasText = taskTitle });
        var editIcon = card.Locator("[data-testid='edit-task-btn']").Or(card.GetByRole(AriaRole.Button, new() { Name = "Edit" })).Or(card.Locator(".edit-icon"));
        await editIcon.ClickAsync();
    }

    public async Task ShuffleTasksAsync()
    {
        await ShuffleButton.ClickAsync();
        // Handle confirmation dialog if it shows up in UI (e.g. system alert or modal popup)
        var confirmBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Shuffle" }).Or(Page.GetByText("Confirm"));
        if (await confirmBtn.IsVisibleAsync())
        {
            await confirmBtn.ClickAsync();
        }
    }
}
