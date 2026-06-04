# Playwright .NET — E2E & API Testing Framework

This repository houses the production-grade test automation framework for **Nest — Proportional Family Task Coordinator**. The framework is built using **Playwright + .NET 8 (C#)**, targeting the Nest web/mobile application backed by Supabase.

---

## Technical Stack

- **Core Framework**: .NET 8 (C#)
- **Test Runner**: NUnit 3
- **Web Automation**: Microsoft.Playwright (Chromium, Firefox, WebKit)
- **API Response Schema Validation**: NJsonSchema (validating dynamically against `swagger.json`)
- **Seeding & Cleanup Tools**: Supabase Data Manager (REST API & admin actions)
- **Reporting**: Allure Reports (`Allure.NUnit`) + Playwright HTML Reports + Trace Viewer
- **Test Coverage Matrix**: A complete matrix of app features and their test coverage is detailed in [Tests.md](_docs/Tests.md).

---

## Solution Architecture

```
Playwright-dotNet/
├── _docs/                          # Specs, swagger.json, [Test Coverage Matrix](_docs/Tests.md)
├── src/
│   ├── PlaywrightFramework.Core/   # Infrastructure layer (configurations, storage caching, base tests)
│   ├── PlaywrightFramework.UI/     # Page Object Models (POMs)
│   ├── PlaywrightFramework.Api/    # REST API Clients & models
│   └── PlaywrightFramework.Tools/  # Swagger Schema Validator & Supabase Seeder
├── tests/
│   ├── E2E.Tests/                  # UI-level end-to-end NUnit tests
│   └── Api.Tests/                  # REST API NUnit tests
├── agents/
│   └── playwright-test-healer/     # AI test-healer prompting templates
└── playwright.config.json          # Playwright global configurations
```

---

## Configuration & Environment Setup

Local settings are defined in `appsettings.json`. The runner will fall back to environment variables in CI/CD environments.

### Local Settings Configuration
Update the following settings to match your target environment:

- `BaseUrl`: URL of the Nest web application (default: `https://even-hen.github.io/nest-family-app`)
- `ApiBaseUrl`: REST API endpoint of your Supabase project (default: `https://tjqznfdufdodbnjwapcv.supabase.co`)
- `SupabaseAnonKey`: Public Supabase anon key
- `SupabaseServiceRoleKey`: Admin service role key (required for automated database seeding and cascade cleanup)
- `Headless`: Toggle headless mode for Playwright browser execution (`true`/`false`)
- `TimeoutSeconds`: Maximum wait time in seconds for elements/responses

---

## Running Tests

Execute commands from the repository root:

### 1. Run all tests with reports
```bash
dotnet test PlaywrightFramework.sln --logger "html;logfilename=test-report.html"
```

### 2. Run API tests only
```bash
dotnet test PlaywrightFramework.sln --filter "Category=api"
```

### 3. Run UI E2E tests only
```bash
dotnet test PlaywrightFramework.sln --filter "Category=e2e"
```

### 4. Run a specific test suite or case
```bash
dotnet test PlaywrightFramework.sln --filter "FullyQualifiedName~OnboardingTests"
```

---

## Seeding & Isolation Design

1. **API Testing Context**: Individual tests spin up dedicated data resources using `SupabaseDataManager` directly via Supabase admin APIs.
2. **UI E2E Storage State Cache**: The framework avoids UI-level login flows for every test by programmatically requesting session tokens via Supabase Auth APIs and injecting them into the browser context. This bypasses the UI authentication screen entirely, avoiding auth rate-limiting. See `AuthFixture.cs`.
3. **Cascading Cleanup Teardown**: Upon test completion (whether successful or failed), `DeleteGroupCascadeAsync` runs to cleanly prune all entries in the correct relational order (`notifications` -> `assignments` -> `invite_links` -> `tasks` -> `users` -> `groups`). The method dynamically supports both **Group IDs** and **User IDs**.
4. **Thread-Safe Parallel Execution**: Test fixtures use `[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]` to ensure NUnit instantiates a fresh class instance per test case. This prevents cross-test state contamination and database seeding collisions during parallel execution.

---

## Trace Analysis & Failure Diagnostics

- **Trace Capture**: If any UI test fails, the framework automatically captures a Playwright trace `.zip` file and failure screenshot.
- **Attachment Path**: Artifacts are saved to the `reports/traces/` and `reports/screenshots/` directories and attached to the NUnit test results.
- **Viewing Traces**: You can inspect the recorded test execution via the Playwright trace viewer:
  ```bash
  npx playwright show-trace path/to/trace.zip
  ```

---

## AI Self-Healing Locators (Test Healer Agent)

If a test fails due to a broken locator or DOM change, you can leverage the **Playwright Test Healer** to propose corrected code:

1. Locate the diagnostic JSON reports in `reports/diagnostics/`.
2. Feed the diagnostic JSON and the target Page Object Model file located under `src/PlaywrightFramework.UI/Pages/` into an LLM using the instructions in `agents/playwright-test-healer/README.md` and `prompt_template.txt`.
3. Apply the proposed C# element locators directly back to your POM page files.