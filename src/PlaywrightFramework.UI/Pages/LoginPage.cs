using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class LoginPage : BasePage
{
    public LoginPage(IPage page) : base(page) { }

    public ILocator EmailInput => Page.Locator("input[type='email']:visible").First;
    public ILocator PasswordInput => Page.Locator("input[type='password']:visible").First;
    public ILocator LoginButton    => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Sign In" })
        .Or(Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }))
        .First;
    public ILocator ErrorMessage   => Page.Locator("[data-testid='error-message']:visible")
        .Or(Page.Locator(".error-message:visible"))
        .Or(Page.GetByText("Invalid login credentials", new() { Exact = false }))
        .Or(Page.GetByText("Invalid email or password", new() { Exact = false }))
        .Or(Page.GetByRole(AriaRole.Alert))
        .First;
    public ILocator RegisterLink   => Page.GetByRole(AriaRole.Link, new() { Name = "Create one" })
        .Or(Page.GetByRole(AriaRole.Link, new() { Name = "Register" }))
        .Or(Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Create one" }))
        .First;

    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }
}