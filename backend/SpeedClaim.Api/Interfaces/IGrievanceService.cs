using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.Grievances;

namespace SpeedClaim.Api.Interfaces;

public interface IGrievanceService
{
    Task<GrievanceDto> RaiseGrievanceAsync(Guid customerId, RaiseGrievanceRequest request);
    Task<IEnumerable<GrievanceDto>> GetMyGrievancesAsync(Guid customerId);
    Task<PagedResponse<GrievanceDto>> GetAllGrievancesAsync(int page, int pageSize);
    Task<GrievanceDto> GetGrievanceByIdAsync(Guid id, Guid? requestingCustomerId = null);
    Task AssignGrievanceAsync(Guid grievanceId, Guid officerId);
    Task UpdateGrievanceStatusAsync(Guid grievanceId, UpdateGrievanceStatusRequest request, Guid actorId);
    Task<string> AttachDocumentAsync(Guid grievanceId, Guid customerId, Microsoft.AspNetCore.Http.IFormFile file);
}
