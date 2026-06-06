using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Claims;

public interface IClaimService
{
    Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ClaimResponse>> GetAllClaimsAsync(Guid userId, int pageNumber, int pageSize);
    Task<IEnumerable<SpeedClaim.Api.Dtos.Claims.ClaimDocumentChecklistDto>> GetClaimChecklistAsync(Guid claimId);
    Task<ClaimResponse> SubmitClaimAsync(Guid userId, SubmitClaimRequest request);
    Task<ClaimResponse> UpdateClaimStatusAsync(Guid claimId, Guid actorId, UpdateClaimStatusRequest request);
}
