using Microsoft.Playwright;
using System.Threading.Tasks;
using PlaywrightFramework.Core.Configuration;

namespace PlaywrightFramework.UI.Pages;

public abstract class BasePage
{
    protected readonly IPage Page;

    protected BasePage(IPage page)
    {
        Page = page;
    }

    public async Task NavigateToAsync(string path)
    {
        var baseUrl = ConfigManager.Settings.BaseUrl.TrimEnd('/');
        var relativePath = path.StartsWith("/") ? path : $"/{path}";
        await Page.GotoAsync($"{baseUrl}{relativePath}");
    }

    public async Task ClickAsync(ILocator locator)
    {
        await locator.ClickAsync();
    }

    public async Task TypeAsync(ILocator locator, string text)
    {
        await locator.FillAsync(text);
    }

    public async Task<bool> IsVisibleAsync(ILocator locator)
    {
        return await locator.IsVisibleAsync();
    }
}
