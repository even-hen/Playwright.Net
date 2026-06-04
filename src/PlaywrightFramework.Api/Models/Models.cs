using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PlaywrightFramework.Api.Models;

public record AuthUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email
);

public record AuthResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("user")] AuthUser User
);

public record UserProfile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("resource")] int Resource,
    [property: JsonPropertyName("group_id")] string? GroupId,
    [property: JsonPropertyName("timezone")] string Timezone,
    [property: JsonPropertyName("notification_time")] string NotificationTime,
    [property: JsonPropertyName("language")] string Language,
    [property: JsonPropertyName("theme")] string Theme,
    [property: JsonPropertyName("expo_push_token")] string? ExpoPushToken,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);

public record FamilyGroup(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("created_by")] string CreatedBy,
    [property: JsonPropertyName("auto_distribution")] bool AutoDistribution,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);

public record ChoreTask(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("group_id")] string GroupId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("emoji")] string Emoji,
    [property: JsonPropertyName("complexity")] int Complexity,
    [property: JsonPropertyName("week_days")] List<int> WeekDays,
    [property: JsonPropertyName("available_for")] List<string> AvailableFor,
    [property: JsonPropertyName("assigned_to")] string? AssignedTo,
    [property: JsonPropertyName("auto")] bool Auto,
    [property: JsonPropertyName("is_active")] bool IsActive,
    [property: JsonPropertyName("created_by")] string CreatedBy,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);

public record TaskAssignment(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("group_id")] string GroupId,
    [property: JsonPropertyName("task_id")] string TaskId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("week_days")] List<int> WeekDays,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("assigned_to")] string? AssignedTo,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("complexity")] int Complexity,
    [property: JsonPropertyName("week_start")] string WeekStart,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);

public record AlertNotification(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("is_read")] bool IsRead,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);

public record InviteLinkToken(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("group_id")] string GroupId,
    [property: JsonPropertyName("expires_at")] DateTime ExpiresAt,
    [property: JsonPropertyName("used_by")] List<string> UsedBy,
    [property: JsonPropertyName("created_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] DateTime? CreatedAt
);
