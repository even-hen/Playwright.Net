using Microsoft.Playwright;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightFramework.Api.Models;

namespace PlaywrightFramework.Api.Clients;

public class AssignmentsClient : BaseApiClient
{
    public AssignmentsClient(IAPIRequestContext requestContext) : base(requestContext) { }

    public async Task<List<TaskAssignment>> GetAssignmentsAsync(string groupId, string? date = null, string? status = null, string? assignedTo = null)
    {
        var url = $"/rest/v1/assignments?group_id=eq.{groupId}&select=*";
        
        if (!string.IsNullOrEmpty(date))
            url += $"&date=eq.{date}";
        if (!string.IsNullOrEmpty(status))
            url += $"&status=eq.{status}";
        if (!string.IsNullOrEmpty(assignedTo))
            url += $"&assigned_to=eq.{assignedTo}";

        var response = await RequestContext.GetAsync(url);
        return await HandleResponseAsync<List<TaskAssignment>>(response, "Assignment");
    }

    public async Task<TaskAssignment> CreateAssignmentAsync(TaskAssignment assignmentInput)
    {
        var response = await RequestContext.PostAsync("/rest/v1/assignments", new()
        {
            DataObject = assignmentInput,
            Headers = new Dictionary<string, string> { { "Prefer", "return=representation" } }
        });

        var list = await HandleResponseAsync<List<TaskAssignment>>(response, "Assignment");
        return list[0];
    }

    public async Task UpdateAssignmentStatusAsync(string id, string status)
    {
        var body = new { status = status };
        var response = await RequestContext.PatchAsync($"/rest/v1/assignments?id=eq.{id}", new()
        {
            DataObject = body
        });
        await EnsureSuccessAsync(response);
    }

    public async Task DeleteAssignmentAsync(string id)
    {
        var response = await RequestContext.DeleteAsync($"/rest/v1/assignments?id=eq.{id}");
        await EnsureSuccessAsync(response);
    }
}
