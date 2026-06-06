namespace SpeedClaim.Api.Models;

public class HealthPolicy : Policy
{
    public bool CoversDental { get; set; }
    public decimal Deductible { get; set; }
    public string NetworkType { get; set; }
}
