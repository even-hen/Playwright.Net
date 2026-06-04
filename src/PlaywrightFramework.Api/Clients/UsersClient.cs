using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class UsersClient : BaseApiClient
{
    public UsersClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<UserProfile>> GetUserProfilesAsync(string? id = null, string? groupId = null, string? email = null)
    {
        var queryParams = new Dictionary<string, object>();
        queryParams["select"] = "*";
        
        if (!string.IsNullOrEmpty(id))
            queryParams["id"] = $"eq.{id}";
        if (!string.IsNullOrEmpty(groupId))
            queryParams["group_id"] = $"eq.{groupId}";
        if (!string.IsNullOrEmpty(email))
            queryParams["email"] = $"eq.{email}";

        var response = await RequestContext.GetAsync("/rest/v1/users", new()
        {
            Params = queryParams
        });

        return await HandleResponseAsync<List<UserProfile>>(response, "User");
    }

    public async Task<UserProfile> GetUserProfileSingleAsync(string id)
    {
        var response = await RequestContext.GetAsync($"/rest/v1/users?id=eq.{id}&select=*");
        var list = await HandleResponseAsync<List<UserProfile>>(response, "User");
        if (list.Count == 0)
        {
            throw new System.Exception($"User profile not found for ID: {id}");
        }
        return list[0];
    }

    public async Task UpdateUserProfileAsync(string id, object updates)
    {
        var response = await RequestContext.PatchAsync($"/rest/v1/users?id=eq.{id}", new()
        {
            DataObject = updates
        });
        await EnsureSuccessAsync(response);
    }
}
