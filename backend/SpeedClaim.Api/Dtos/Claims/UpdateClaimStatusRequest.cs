using System;

namespace SpeedClaim.Api.Dtos.Claims;

public class UpdateClaimStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public decimal? ApprovedAmount { get; set; }
}
