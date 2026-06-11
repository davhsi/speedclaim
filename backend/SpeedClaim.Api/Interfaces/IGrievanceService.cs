using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Grievances;

namespace SpeedClaim.Api.Interfaces;

public interface IGrievanceService
{
    Task<GrievanceDto> RaiseGrievanceAsync(Guid customerId, RaiseGrievanceRequest request);
    Task<IEnumerable<GrievanceDto>> GetMyGrievancesAsync(Guid customerId);
    Task<IEnumerable<GrievanceDto>> GetAllGrievancesAsync();
    Task<GrievanceDto> GetGrievanceByIdAsync(Guid id);
    Task AssignGrievanceAsync(Guid grievanceId, Guid officerId);
    Task UpdateGrievanceStatusAsync(Guid grievanceId, UpdateGrievanceStatusRequest request);
}
