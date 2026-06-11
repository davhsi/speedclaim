using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Catalog;

public record DocumentRequirementDto(
    EntityType EntityType,
    string Domain,
    string DocumentKey,
    string Label,
    string Description,
    bool IsMandatory,
    bool IsActive
);

public record DocumentRequirementResponseDto(
    Guid Id,
    Guid? ProductId,
    EntityType EntityType,
    string Domain,
    string DocumentKey,
    string Label,
    string Description,
    bool IsMandatory,
    bool IsActive
);

public record UpdateDocumentRequirementsRequest(
    List<DocumentRequirementDto> Requirements
);
