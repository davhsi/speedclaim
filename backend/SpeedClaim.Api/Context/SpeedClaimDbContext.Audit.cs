using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SpeedClaim.Api.Models;
using System.Text.Json;

namespace SpeedClaim.Api.Context;

public partial class SpeedClaimDbContext
{
    /// <summary>
    /// Explicit actor override for the generic change-tracker audit (see OnBeforeSaveChanges).
    /// Set via IUnitOfWork.SetCurrentActor when a request mutates a User row before that user
    /// has a JWT on the current HttpContext (e.g. during login), so the audit entry isn't left
    /// with a blank actor. Takes priority over the HttpContext claims lookup when set.
    /// </summary>
    public Guid? CurrentActorOverride { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaveChanges();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        OnBeforeSaveChanges();
        return base.SaveChanges();
    }

    // Internal infrastructure entities — too noisy; semantic audit entries already cover
    // the meaningful events (UserLoggedIn/Out, ClaimStatusChanged, etc.).
    private static readonly HashSet<Type> _auditExclusions = new()
    {
        typeof(Session),
        typeof(UserToken),
        typeof(ProcessedWebhookEvent),
        typeof(Notification),
        typeof(EmailLog),
        typeof(ClaimStatusHistory),
        typeof(PolicyStatusHistory),
        typeof(PremiumSchedule),
        typeof(PremiumPayment),
        // Customer changes are captured by the ProfileUpdated semantic entry with human-readable field names
        typeof(Customer),
    };

    private void OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditEntry>();
        var httpContext = _httpContextAccessor?.HttpContext;
        var userIdString = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = CurrentActorOverride;
        if (userId == null && Guid.TryParse(userIdString, out var parsedUserId))
        {
            userId = parsedUserId;
        }
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;
            if (_auditExclusions.Contains(entry.Entity.GetType()))
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                UserId = userId,
                IpAddress = ipAddress,
                Action = entry.State.ToString()
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;

                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries)
        {
            AuditLogs.Add(auditEntry.ToAudit());
        }
    }
}

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> KeyValues { get; } = new Dictionary<string, object?>();
    public Dictionary<string, object?> OldValues { get; } = new Dictionary<string, object?>();
    public Dictionary<string, object?> NewValues { get; } = new Dictionary<string, object?>();

    public AuditLog ToAudit()
    {
        var audit = new AuditLog
        {
            UserId = UserId,
            IpAddress = IpAddress,
            EntityType = TableName,
            Action = Action,
            CreatedAt = DateTime.UtcNow,
            EntityId = KeyValues.Count > 0 && Guid.TryParse(KeyValues.Values.FirstOrDefault()?.ToString(), out var parsedId) ? parsedId : Guid.Empty
        };
        
        if (OldValues.Count > 0)
        {
            audit.OldValue = JsonSerializer.Serialize(OldValues);
        }
        
        if (NewValues.Count > 0)
        {
            audit.NewValue = JsonSerializer.Serialize(NewValues);
        }

        return audit;
    }
}
