using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using PlaywrightFramework.Core.Configuration;

namespace PlaywrightFramework.Tools.SupabaseDataManager;

public class SupabaseDataManager
{
    private static readonly HttpClient _client;
    private readonly string _serviceRoleKey;
    private readonly string _baseUrl;

    static SupabaseDataManager()
    {
        _client = new HttpClient();
        var settings = ConfigManager.Settings;
        _client.DefaultRequestHeaders.Add("apikey", settings.SupabaseServiceRoleKey);
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.SupabaseServiceRoleKey}");
    }

    public SupabaseDataManager()
    {
        var settings = ConfigManager.Settings;
        _serviceRoleKey = settings.SupabaseServiceRoleKey;
        _baseUrl = settings.ApiBaseUrl.TrimEnd('/');
    }

    // High level: Create a group with default tasks and some members
    public async Task<GroupCreationResult> CreateTestGroupSetupAsync(string groupName, List<UserSetupSpec> memberSpecs)
    {
        // Ensure at least one user spec to act as admin/creator
        if (memberSpecs == null || memberSpecs.Count == 0)
            throw new ArgumentException("memberSpecs must contain at least one user (the admin).", nameof(memberSpecs));

        // 1. Create the admin user (first spec)
        var adminSpec = memberSpecs[0];
        var adminUserId = await CreateAuthUserAdminAsync(adminSpec.Email, adminSpec.Password, adminSpec.Name);
        // Create admin profile without group yet; we'll link after group creation
        await CreateUserProfileAsync(adminUserId, adminSpec.Email, adminSpec.Name, adminSpec.Type, adminSpec.Resource, null);

        // 2. Create the group, passing adminUserId as created_by
        var groupId = await CreateGroupAsync(groupName, adminUserId);

        // 3. Link admin user to the new group (mirrors UI behaviour)
        var adminProfileUpdate = new { group_id = groupId };
        var patchUrl = $"{_baseUrl}/rest/v1/users?id=eq.{adminUserId}";
        var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(adminProfileUpdate), Encoding.UTF8, "application/json")
        };
        var patchResponse = await _client.SendAsync(patchRequest);
        if (!patchResponse.IsSuccessStatusCode)
        {
            var err = await patchResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to link admin to group. Status: {patchResponse.StatusCode}, Error: {err}");
        }

        // 4. Create remaining users and add them to the group
        var createdUserIds = new List<string> { adminUserId };
        for (int i = 1; i < memberSpecs.Count; i++)
        {
            var spec = memberSpecs[i];
            var userId = await CreateAuthUserAdminAsync(spec.Email, spec.Password, spec.Name);
            await CreateUserProfileAsync(userId, spec.Email, spec.Name, spec.Type, spec.Resource, groupId);
            createdUserIds.Add(userId);
        }

        return new GroupCreationResult
        {
            GroupId = groupId,
            UserIds = createdUserIds
        };
    }

    public async Task<string> CreateGroupAsync(string name, string createdBy)
    {
        var url = $"{_baseUrl}/rest/v1/groups";
        var body = new
        {
            name = name,
            created_by = createdBy
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        // Prefer return=representation to get the created group ID
        request.Headers.Add("Prefer", "return=representation");

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create group. Status: {response.StatusCode}, Error: {err}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement[0].GetProperty("id").GetString() ?? throw new Exception("Group ID not returned.");
    }

    public async Task<string> CreateAuthUserAdminAsync(string email, string password, string name)
    {
        var url = $"{_baseUrl}/auth/v1/admin/users";
        var body = new
        {
            email = email,
            password = password,
            email_confirm = true,
            user_metadata = new { name = name }
        };

        var response = await _client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create auth user via Admin API. Status: {response.StatusCode}, Error: {err}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("id").GetString() ?? throw new Exception("User ID not returned.");
    }

    public async Task CreateUserProfileAsync(string id, string email, string name, string type, int resource, string? groupId)
    {
        var url = $"{_baseUrl}/rest/v1/users";
        var body = new
        {
            id = id,
            email = email,
            name = name,
            type = type,
            resource = resource,
            group_id = groupId,
            timezone = "America/New_York",
            notification_time = "09:00",
            language = "en",
            theme = "light"
        };

        var response = await _client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create user profile. Status: {response.StatusCode}, Error: {err}");
        }
    }

    // Cascading deletion to cleanly wipe a group and all its related records.
    // Handles both Group IDs and User IDs dynamically to ensure complete teardown.
    public async Task DeleteGroupCascadeAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        bool userExists = await CheckUserExistsAsync(id);
        bool groupExists = await CheckGroupExistsAsync(id);

        if (userExists)
        {
            var userId = id;
            var userGroupId = await GetUserGroupIdAsync(userId);
            var createdGroupIds = await GetGroupsCreatedByUserAsync(userId);

            var groupsToClean = new HashSet<string>();
            if (!string.IsNullOrEmpty(userGroupId))
            {
                groupsToClean.Add(userGroupId);
            }
            foreach (var gId in createdGroupIds)
            {
                groupsToClean.Add(gId);
            }

            foreach (var gId in groupsToClean)
            {
                await CleanGroupIdCascadeInternalAsync(gId);
            }

            // Always make sure the user profile and auth account themselves are deleted
            await CleanUserIdOnlyInternalAsync(userId);
        }
        else if (groupExists)
        {
            await CleanGroupIdCascadeInternalAsync(id);
        }
        else
        {
            // Fallback: try to clean as both if not found in db (might have been partially deleted)
            await CleanGroupIdCascadeInternalAsync(id);
            await CleanUserIdOnlyInternalAsync(id);
        }
    }

    private async Task<bool> CheckGroupExistsAsync(string id)
    {
        var url = $"{_baseUrl}/rest/v1/groups?id=eq.{id}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return false;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0;
    }

    private async Task<bool> CheckUserExistsAsync(string id)
    {
        var url = $"{_baseUrl}/rest/v1/users?id=eq.{id}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return false;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0;
    }

    private async Task<string?> GetUserGroupIdAsync(string userId)
    {
        var url = $"{_baseUrl}/rest/v1/users?id=eq.{userId}&select=group_id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            var element = doc.RootElement[0];
            if (element.TryGetProperty("group_id", out var groupProp) && groupProp.ValueKind == JsonValueKind.String)
            {
                return groupProp.GetString();
            }
        }
        return null;
    }

    private async Task<List<string>> GetGroupsCreatedByUserAsync(string userId)
    {
        var url = $"{_baseUrl}/rest/v1/groups?created_by=eq.{userId}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<string>();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var list = new List<string>();
        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                list.Add(element.GetProperty("id").GetString()!);
            }
        }
        return list;
    }

    private async Task<string?> GetGroupCreatorIdAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/groups?id=eq.{groupId}&select=created_by";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            var element = doc.RootElement[0];
            if (element.TryGetProperty("created_by", out var creatorProp) && creatorProp.ValueKind == JsonValueKind.String)
            {
                return creatorProp.GetString();
            }
        }
        return null;
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email)) return null;
        var url = $"{_baseUrl}/rest/v1/users?email=eq.{email}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
        {
            return doc.RootElement[0].GetProperty("id").GetString();
        }
        return null;
    }

    private async Task CleanGroupIdCascadeInternalAsync(string groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return;

        // 1. Get all user IDs associated with this group
        var userIds = await GetGroupUserIdsAsync(groupId);

        // Also check group creator (ensure they are cleaned up even if group_id is null/not linked)
        var creatorId = await GetGroupCreatorIdAsync(groupId);
        if (!string.IsNullOrEmpty(creatorId) && !userIds.Contains(creatorId))
        {
            userIds.Add(creatorId);
        }

        // 2. Delete notifications and assignments for these users
        foreach (var userId in userIds)
        {
            await DeleteNotificationsForUserAsync(userId);
            await DeleteAssignmentsForUserAsync(userId);
        }

        // 3. Get all tasks for this group
        var taskIds = await GetGroupTaskIdsAsync(groupId);

        // 4. Delete assignments for these tasks
        foreach (var taskId in taskIds)
        {
            await DeleteAssignmentsForTaskAsync(taskId);
        }

        // 5. Delete invite links for the group
        await DeleteInviteLinksForGroupAsync(groupId);

        // 6. Delete tasks for the group
        await DeleteTasksForGroupAsync(groupId);

        // 7. Break circular references: set group_id to null for these users
        foreach (var userId in userIds)
        {
            await ClearUserGroupIdAsync(userId);
        }

        // 8. Delete the group itself (now that user group_id references are severed)
        await DeleteGroupOnlyAsync(groupId);

        // 9. Delete user profiles (now that the group referencing them as creator is gone)
        foreach (var userId in userIds)
        {
            await DeleteUserProfileAsync(userId);
        }

        // 10. Delete users from Supabase Auth admin API
        foreach (var userId in userIds)
        {
            await DeleteAuthUserAdminAsync(userId);
        }
    }

    private async Task CleanUserIdOnlyInternalAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        await DeleteNotificationsForUserAsync(userId);
        await DeleteAssignmentsForUserAsync(userId);
        await DeleteUserProfileAsync(userId);
        await DeleteAuthUserAdminAsync(userId);
    }

    private async Task<List<string>> GetGroupUserIdsAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/users?group_id=eq.{groupId}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<string>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var list = new List<string>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            list.Add(element.GetProperty("id").GetString()!);
        }
        return list;
    }

    private async Task<List<string>> GetGroupTaskIdsAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/tasks?group_id=eq.{groupId}&select=id";
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<string>();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var list = new List<string>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            list.Add(element.GetProperty("id").GetString()!);
        }
        return list;
    }

    private async Task DeleteNotificationsForUserAsync(string userId)
    {
        var url = $"{_baseUrl}/rest/v1/notifications?user_id=eq.{userId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete notifications for user {userId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteAssignmentsForTaskAsync(string taskId)
    {
        var url = $"{_baseUrl}/rest/v1/assignments?task_id=eq.{taskId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete assignments for task {taskId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteAssignmentsForUserAsync(string userId)
    {
        var url = $"{_baseUrl}/rest/v1/assignments?assigned_to=eq.{userId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete assignments for user {userId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteInviteLinksForGroupAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/invite_links?group_id=eq.{groupId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete invite links for group {groupId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteTasksForGroupAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/tasks?group_id=eq.{groupId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete tasks for group {groupId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteUserProfileAsync(string userId)
    {
        var url = $"{_baseUrl}/rest/v1/users?id=eq.{userId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete user profile for user {userId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteAuthUserAdminAsync(string userId)
    {
        var url = $"{_baseUrl}/auth/v1/admin/users/{userId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete auth user {userId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task DeleteGroupOnlyAsync(string groupId)
    {
        var url = $"{_baseUrl}/rest/v1/groups?id=eq.{groupId}";
        var response = await _client.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to delete group {groupId}. Status: {response.StatusCode}, Content: {content}");
        }
    }

    private async Task ClearUserGroupIdAsync(string userId)
    {
        var update = new { group_id = (string?)null };
        var url = $"{_baseUrl}/rest/v1/users?id=eq.{userId}";
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(JsonSerializer.Serialize(update), Encoding.UTF8, "application/json")
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Cleanup WARNING] Failed to clear group_id for user {userId}. Status: {response.StatusCode}, Content: {content}");
        }
    }
}

public class UserSetupSpec
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Adult";
    public int Resource { get; set; } = 100;
}

public class GroupCreationResult
{
    public string GroupId { get; set; } = string.Empty;
    public List<string> UserIds { get; set; } = new();
}
