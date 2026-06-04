using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class TabBar : BasePage
{
    public TabBar(IPage page) : base(page) { }

    public ILocator ToDoTab => Page.Locator("[data-testid='tab-todo']:visible").Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "To Do" })).Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Assignments" })).First;
    public ILocator MembersTab => Page.Locator("[data-testid='tab-members']:visible").Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Members" })).First;
    public ILocator TasksTab => Page.Locator("[data-testid='tab-tasks']:visible").Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Tasks" })).Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Schedule" })).First;
    public ILocator AlertsTab => Page.Locator("[data-testid='tab-alerts']:visible").Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Alerts" })).Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Notifications" })).First;
    public ILocator SettingsTab => Page.Locator("[data-testid='tab-settings']:visible").Or(Page.Locator("[role='tab']:visible").Filter(new() { HasText = "Settings" })).First;

    public async Task NavigateToToDoAsync() => await ToDoTab.ClickAsync();
    public async Task NavigateToMembersAsync() => await MembersTab.ClickAsync();
    public async Task NavigateToTasksAsync() => await TasksTab.ClickAsync();
    public async Task NavigateToAlertsAsync() => await AlertsTab.ClickAsync();
    public async Task NavigateToSettingsAsync() => await SettingsTab.ClickAsync();
}
