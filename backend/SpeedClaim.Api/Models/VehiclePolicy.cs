namespace SpeedClaim.Api.Models;

public class VehiclePolicy : Policy
{
    public string VehicleNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int ManufactureYear { get; set; }
    public decimal InsuredDeclaredValue { get; set; }
    public bool IsComprehensive { get; set; }
}
