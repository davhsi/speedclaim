using System;

namespace SpeedClaim.Api.Dtos.SystemManagement;

public record SystemConfigDto(
    string ConfigKey,
    string ConfigValue,
    string? Description,
    DateTimeOffset? UpdatedAt
);

public record AuditLogDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    Guid? UserId,
    string? UserName,
    string? IpAddress,
    DateTime CreatedAt
);

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTimeOffset CreatedAt
);

public record UpdateSystemConfigRequest(
    string ConfigKey,
    string ConfigValue
);

public record ManageEmailTemplateRequest(
    string TemplateKey,
    string Subject,
    string BodyHtml
);

public record EmailTemplateDto(
    Guid Id,
    string TemplateKey,
    string Subject,
    string BodyHtml,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
