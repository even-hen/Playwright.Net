# Playwright Test Healer agent

This tool assists QA Automation engineers in self-healing broken locators in Playwright .NET Page Object Model (POM) files using AI analysis.

## How it works

1. **Failure Diagnostics**: When a UI E2E test fails locally due to a locator timeout, the framework captures the surrounding DOM tree and details of the failed locator in `reports/diagnostics/` as a JSON file.
2. **AI Healing Prompt**: You pass the diagnostics JSON file and the target C# Page Object Model file to the healer agent.
3. **Suggested Fix**: The agent analyzes the DOM structure, maps the target element, and outputs the corrected C# locator.

## Diagnostics JSON Format

```json
{
  "testName": "TC_E2E_017_CreateNewTask_ViaModal",
  "failedLocator": "Page.Locator(\"[data-testid='add-task-btn']\")",
  "exceptionMessage": "Timeout 10000ms exceeded while waiting for element to be visible",
  "targetAction": "ClickAsync",
  "pomFile": "src/PlaywrightFramework.UI/Pages/TasksPage.cs",
  "domFragment": "<body><div id='app'><button class='btn-add-chore' id='btn-add'>+ Add Task</button></div></body>"
}
```

## Running the Healer

Use the prompt template located in this folder `prompt_template.txt` with your AI coding assistant (Claude, Copilot, Cursor) to generate the fix:

```bash
# Example usage:
Feed the contents of reports/diagnostics/failure.json and the POM file to the AI with prompt_template.txt.
```
