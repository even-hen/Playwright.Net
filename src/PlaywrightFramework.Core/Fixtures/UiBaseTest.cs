using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using PlaywrightFramework.Core.Configuration;
using Allure.NUnit;
using Allure.Net.Commons;

namespace PlaywrightFramework.Core.Fixtures;

[AllureNUnit]
public class UiBaseTest : PageTest
{
    static UiBaseTest()
    {
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports", "allure-results"));
    }

    protected IBrowserContext? CustomContext { get; set; }
    protected IPage? CustomPage { get; set; }

    public IPage CurrentPage => CustomPage ?? Page;
    public IBrowserContext CurrentContext => CustomContext ?? Context;

    protected async Task<IPage> SetupCustomContextAsync(BrowserNewContextOptions options)
    {
        CustomContext = await Browser.NewContextAsync(options);
        CustomContext.SetDefaultTimeout(ConfigManager.Settings.TimeoutSeconds * 1000);
        await CustomContext.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        CustomPage = await CustomContext.NewPageAsync();
        return CustomPage;
    }

    [SetUp]
    public async Task SetupUiTest()
    {
        // Set default timeout from configuration
        Context.SetDefaultTimeout(ConfigManager.Settings.TimeoutSeconds * 1000);
        
        // Start tracing for visual debugging
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    [TearDown]
    public async Task TeardownUiTest()
    {
        var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
        var testName = TestContext.CurrentContext.Test.Name;
        
        // Capture trace/screenshot on failure using current active page/context
        var pageToTearDown = CurrentPage;
        var contextToTearDown = CurrentContext;

        if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            var reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
            var tracePath = Path.Combine(reportsDir, "traces", $"{testName}.zip");
            var screenshotPath = Path.Combine(reportsDir, "screenshots", $"{testName}.png");
            
            Directory.CreateDirectory(Path.GetDirectoryName(tracePath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);

            // Capture screenshot
            try
            {
                await pageToTearDown.ScreenshotAsync(new() { Path = screenshotPath });
                TestContext.AddTestAttachment(screenshotPath, "Failure Screenshot");
                AllureApi.AddAttachment("Failure Screenshot", "image/png", screenshotPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
            }

            // Capture trace
            try
            {
                await contextToTearDown.Tracing.StopAsync(new() { Path = tracePath });
                TestContext.AddTestAttachment(tracePath, "Playwright Trace");
                AllureApi.AddAttachment("Playwright Trace", "application/zip", tracePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to capture trace: {ex.Message}");
            }
        }
        else
        {
            try
            {
                await contextToTearDown.Tracing.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to stop tracing: {ex.Message}");
            }
        }

        // Clean up custom context if it was created
        if (CustomContext != null)
        {
            try
            {
                await CustomContext.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to dispose custom context: {ex.Message}");
            }
            CustomContext = null;
            CustomPage = null;
        }
    }
}
