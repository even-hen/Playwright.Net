using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class GroupsClient : BaseApiClient
{
    public GroupsClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<FamilyGroup>> GetGroupsAsync(string? id = null)
    {
        var url = "/rest/v1/groups?select=*";
        if (!string.IsNullOrEmpty(id))
        {
            url += $"&id=eq.{id}";
        }

        var response = await RequestContext.GetAsync(url);
        return await HandleResponseAsync<List<FamilyGroup>>(response, "Group");
    }

    public async Task<FamilyGroup> GetGroupSingleAsync(string id)
    {
        var list = await GetGroupsAsync(id);
        if (list.Count == 0)
        {
            throw new System.Exception($"Group not found for ID: {id}");
        }
        return list[0];
    }

    public async Task<FamilyGroup> CreateGroupAsync(string name, string createdBy, bool autoDistribution = true)
    {
        var body = new
        {
            name = name,
            created_by = createdBy,
            auto_distribution = autoDistribution
        };

        var response = await RequestContext.PostAsync("/rest/v1/groups", new()
        {
            DataObject = body,
            Headers = new Dictionary<string, string> { { "Prefer", "return=representation" } }
        });

        var list = await HandleResponseAsync<List<FamilyGroup>>(response, "Group");
        var group = list[0];

        // Manually link the creator user to the group to match application behavior
        var updateProfileBody = new { group_id = group.Id };
        var patchResponse = await RequestContext.PatchAsync($"/rest/v1/users?id=eq.{createdBy}", new()
        {
            DataObject = updateProfileBody
        });
        await EnsureSuccessAsync(patchResponse);

        return group;
    }

    public async Task UpdateGroupSettingsAsync(string id, object updates)
    {
        var response = await RequestContext.PatchAsync($"/rest/v1/groups?id=eq.{id}", new()
        {
            DataObject = updates
        });
        await EnsureSuccessAsync(response);
    }
}
