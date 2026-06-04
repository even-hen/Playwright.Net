using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class NotificationsClient : BaseApiClient
{
    public NotificationsClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<AlertNotification>> GetNotificationsAsync(string userId, bool? isRead = null)
    {
        var url = $"/rest/v1/notifications?user_id=eq.{userId}&select=*";
        if (isRead.HasValue)
        {
            url += $"&is_read=eq.{isRead.Value.ToString().ToLower()}";
        }

        var response = await RequestContext.GetAsync(url);
        return await HandleResponseAsync<List<AlertNotification>>(response, "Notification");
    }

    public async Task UpdateNotificationReadStateAsync(string id, bool isRead)
    {
        var body = new { is_read = isRead };
        var response = await RequestContext.PatchAsync($"/rest/v1/notifications?id=eq.{id}", new()
        {
            DataObject = body
        });
        await EnsureSuccessAsync(response);
    }

    public async Task MarkAllNotificationsAsReadAsync(string userId)
    {
        var body = new { is_read = true };
        var response = await RequestContext.PatchAsync($"/rest/v1/notifications?user_id=eq.{userId}", new()
        {
            DataObject = body
        });
        await EnsureSuccessAsync(response);
    }
}
