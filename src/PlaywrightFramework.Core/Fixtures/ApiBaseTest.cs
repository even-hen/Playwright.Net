using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightFramework.Core.Configuration;
using Allure.NUnit;
using System;
using System.IO;

namespace PlaywrightFramework.Core.Fixtures;

[AllureNUnit]
public class ApiBaseTest : PlaywrightTest
{
    static ApiBaseTest()
    {
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports", "allure-results"));
    }

    protected IPlaywright LocalPlaywright { get; private set; } = null!;
    protected IAPIRequestContext ApiRequestContext { get; private set; } = null!;

    [SetUp]
    public async Task SetupApiTest()
    {
        var headers = new Dictionary<string, string>
        {
            { "apikey", ConfigManager.Settings.SupabaseAnonKey },
            { "Content-Type", "application/json" }
        };

        LocalPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        ApiRequestContext = await LocalPlaywright.APIRequest.NewContextAsync(new()
        {
            BaseURL = ConfigManager.Settings.ApiBaseUrl,
            ExtraHTTPHeaders = headers,
            Timeout = ConfigManager.Settings.TimeoutSeconds * 1000
        });
    }

    [TearDown]
    public async Task TeardownApiTest()
    {
        if (ApiRequestContext != null)
        {
            await ApiRequestContext.DisposeAsync();
        }
        if (LocalPlaywright != null)
        {
            LocalPlaywright.Dispose();
        }
    }
}