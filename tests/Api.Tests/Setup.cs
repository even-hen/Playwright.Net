using NUnit.Framework;
using PlaywrightFramework.Api.Clients;
using PlaywrightFramework.Tools.SchemaValidator;

namespace Api.Tests;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "reports", "allure-results"));

        BaseApiClient.SchemaValidatorHook = (json, schemaName) =>
        {
            var result = JsonSchemaValidator.Validate(json, schemaName);
            if (!result.IsValid)
            {
                throw new System.Exception($"Schema validation failed for schema '{schemaName}'. Errors: {result.ErrorSummary}");
            }
        };
    }
}
