using Microsoft.Playwright;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class AuthClient : BaseApiClient
{
    public AuthClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<AuthResponse> SignUpAsync(string email, string password, object? metadata = null)
    {
        var body = new
        {
            email = email,
            password = password,
            data = metadata
        };

        var response = await RequestContext.PostAsync("/auth/v1/signup", new()
        {
            DataObject = body
        });

        return await HandleResponseAsync<AuthResponse>(response, "AuthResponse");
    }

    public async Task<AuthResponse> SignInAsync(string email, string password)
    {
        var body = new
        {
            email = email,
            password = password
        };

        var response = await RequestContext.PostAsync("/auth/v1/token?grant_type=password", new()
        {
            DataObject = body
        });

        return await HandleResponseAsync<AuthResponse>(response, "AuthResponse");
    }

    public async Task SignOutAsync()
    {
        var response = await RequestContext.PostAsync("/auth/v1/logout");
        await EnsureSuccessAsync(response);
    }

    public async Task RequestPasswordRecoveryAsync(string email)
    {
        var body = new { email = email };
        var response = await RequestContext.PostAsync("/auth/v1/recover", new()
        {
            DataObject = body
        });
        await EnsureSuccessAsync(response);
    }
}
