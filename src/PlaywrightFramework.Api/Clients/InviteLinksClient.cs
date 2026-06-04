using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class InviteLinksClient : BaseApiClient
{
    public InviteLinksClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<InviteLinkToken>> GetInviteLinksAsync(string? token = null)
    {
        var url = "/rest/v1/invite_links?select=*";
        if (!string.IsNullOrEmpty(token))
        {
            url += $"&token=eq.{token}";
        }

        var response = await RequestContext.GetAsync(url);
        return await HandleResponseAsync<List<InviteLinkToken>>(response, "InviteLink");
    }

    public async Task<InviteLinkToken> GetInviteLinkSingleAsync(string token)
    {
        var list = await GetInviteLinksAsync(token);
        if (list.Count == 0)
        {
            throw new System.Exception($"Invite link not found for token: {token}");
        }
        return list[0];
    }

    public async Task<InviteLinkToken> CreateInviteLinkAsync(string token, string groupId, System.DateTime expiresAt)
    {
        var body = new
        {
            token = token,
            group_id = groupId,
            expires_at = expiresAt,
            used_by = new List<string>()
        };

        var response = await RequestContext.PostAsync("/rest/v1/invite_links", new()
        {
            DataObject = body,
            Headers = new Dictionary<string, string> { { "Prefer", "return=representation" } }
        });

        var list = await HandleResponseAsync<List<InviteLinkToken>>(response, "InviteLink");
        return list[0];
    }

    public async Task UpdateInviteLinkUsageAsync(string token, List<string> usedBy)
    {
        var body = new { used_by = usedBy };
        var response = await RequestContext.PatchAsync($"/rest/v1/invite_links?token=eq.{token}", new()
        {
            DataObject = body
        });
        await EnsureSuccessAsync(response);
    }
}
