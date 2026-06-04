using Microsoft.Playwright;
using System.Threading.Tasks;

namespace PlaywrightFramework.UI.Pages;

public class RegisterPage : BasePage
{
    public RegisterPage(IPage page) : base(page) { }

    public ILocator NameInput => Page.Locator("input[placeholder='Your name']:visible").Or(Page.Locator("input[placeholder='Full Name']:visible")).Or(Page.Locator("input[name='name']:visible")).First;
    public ILocator EmailInput => Page.Locator("input[type='email']:visible").First;
    public ILocator PasswordInput => Page.Locator("input[type='password']:visible").First;
    public ILocator RoleDropdown => Page.GetByRole(AriaRole.Combobox).Or(Page.Locator("select[name='type']")).Or(Page.Locator("[data-testid='role-selector']"));
    public ILocator SliderTrack => Page.Locator("[data-testid='capacity-slider-track']").Or(Page.Locator(".slider-track"));
    public ILocator SliderHandle => Page.Locator("[data-testid='capacity-slider-handle']").Or(Page.Locator(".slider-handle")).Or(Page.Locator("input[type='range']"));
    public ILocator RegisterButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Create Account" }).First;
    public ILocator BackButton => Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Back" }).Or(Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "←" })).Or(Page.Locator("div[tabindex='0']:visible").Filter(new() { HasText = "Sign In" })).First;

    public async Task RegisterAsync(string name, string email, string password, string roleType, int capacity)
    {
        await NameInput.FillAsync(name);
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        
        // Select Role
        if (await RoleDropdown.IsVisibleAsync())
        {
            await RoleDropdown.SelectOptionAsync(roleType);
        }
        else
        {
            // If it's custom day chips or selectors, click on the text role name
            await Page.GetByText(roleType, new() { Exact = true }).ClickAsync();
        }

        // Set Slider Capacity
        await SetSliderCapacityAsync(capacity);

        await RegisterButton.ClickAsync();
    }

    public async Task SetSliderCapacityAsync(int capacity)
    {
        var handle = SliderHandle;
        if (await handle.CountAsync() == 0) return;

        // If it's a native range slider
        var type = await handle.GetAttributeAsync("type");
        if (type == "range")
        {
            await handle.FillAsync(capacity.ToString());
            await handle.EvaluateAsync("e => e.dispatchEvent(new Event('change', { bubbles: true }))");
            return;
        }

        // Else, drag-and-drop simulate using bounding box
        var boundingBox = await handle.BoundingBoxAsync();
        var trackBox = await SliderTrack.BoundingBoxAsync();

        if (boundingBox != null && trackBox != null)
        {
            // Calculate starting X and ending X based on capacity percentage (0 to 100)
            double percentage = capacity / 100.0;
            double targetX = trackBox.X + (trackBox.Width * percentage);
            double centerY = boundingBox.Y + (boundingBox.Height / 2);

            await Page.Mouse.MoveAsync((float)(boundingBox.X + (boundingBox.Width / 2)), (float)centerY);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync((float)targetX, (float)centerY);
            await Page.Mouse.UpAsync();
        }
    }
}
