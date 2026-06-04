using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class NotificationsPage : BasePage
{
    public NotificationsPage(IPage page) : base(page) { }

    public ILocator MarkAllReadButton => Page.Locator("[data-testid='mark-all-read-btn']").Or(Page.GetByText("Mark all read"));
    public ILocator UnreadCountHeader => Page.Locator("[data-testid='unread-count-header']").Or(Page.Locator(".unread-count")).Or(Page.GetByText("unread"));
    public ILocator NotificationItems => Page.Locator("[data-testid='notification-card']").Or(Page.Locator(".notification-card"));
    public ILocator EmptyStateText => Page.GetByText("All quiet").Or(Page.Locator("[data-testid='notifications-empty']"));

    public async Task ClickMarkAllReadAsync()
    {
        await MarkAllReadButton.ClickAsync();
    }

    public async Task TapNotificationAsync(int index)
    {
        await NotificationItems.Nth(index).ClickAsync();
    }
}
