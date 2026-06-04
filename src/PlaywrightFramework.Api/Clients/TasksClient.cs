using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class TasksClient : BaseApiClient
{
    public TasksClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<ChoreTask>> GetTasksAsync(string groupId, bool? isActive = null)
    {
        var url = $"/rest/v1/tasks?group_id=eq.{groupId}&select=*";
        if (isActive.HasValue)
        {
            url += $"&is_active=eq.{isActive.Value.ToString().ToLower()}";
        }

        var response = await RequestContext.GetAsync(url);
        return await HandleResponseAsync<List<ChoreTask>>(response, "Task");
    }

    public async Task<ChoreTask> CreateTaskAsync(ChoreTask taskInput)
    {
        var response = await RequestContext.PostAsync("/rest/v1/tasks", new()
        {
            DataObject = taskInput,
            Headers = new Dictionary<string, string> { { "Prefer", "return=representation" } }
        });

        var list = await HandleResponseAsync<List<ChoreTask>>(response, "Task");
        return list[0];
    }

    public async Task UpdateTaskAsync(string id, object updates)
    {
        var response = await RequestContext.PatchAsync($"/rest/v1/tasks?id=eq.{id}", new()
        {
            DataObject = updates
        });
        await EnsureSuccessAsync(response);
    }

    public async Task DeleteTaskAsync(string id)
    {
        var response = await RequestContext.DeleteAsync($"/rest/v1/tasks?id=eq.{id}");
        await EnsureSuccessAsync(response);
    }
}
