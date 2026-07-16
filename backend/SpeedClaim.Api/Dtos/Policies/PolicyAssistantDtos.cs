using System.Text.Json.Serialization;

namespace SpeedClaim.Api.Dtos.Policies;

public record CreatePolicyAssistantConversationRequest();
public record SendPolicyAssistantMessageRequest(string Question);

public record PolicyAssistantCitationDto(int Index, int PageNumber, string? SectionTitle, string? ClauseReference, string Excerpt);

public record PolicyAssistantMessageDto(
    Guid Id, string Role, string Content, string? EvidenceStatus,
    IReadOnlyList<PolicyAssistantCitationDto> Citations, DateTimeOffset CreatedAt);

public record PolicyAssistantConversationDto(
    Guid Id, Guid PolicyId, Guid BrochureId, string BrochureVersion, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, IReadOnlyList<PolicyAssistantMessageDto>? Messages = null);

public record PolicyAssistantAvailabilityDto(bool Available, string State, string? BrochureVersion, DateOnly? EffectiveFrom);

public record PolicyAssistantAnswerDto(
    Guid RequestId, Guid ConversationId, Guid MessageId, string Answer, string EvidenceStatus,
    string BrochureVersion, IReadOnlyList<PolicyAssistantCitationDto> Citations);

public record PolicyQaRequest(Guid RequestId, Guid BrochureId, Guid ProductId, string BrochureVersion, string Question);
public record PolicyQaResponse(
    Guid RequestId, string Answer, string EvidenceStatus, string BrochureVersion,
    IReadOnlyList<PolicyAssistantCitationDto> Citations, string PromptVersion,
    [property: JsonPropertyName("provider")] string? Provider,
    [property: JsonPropertyName("model")] string? Model);
