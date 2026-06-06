namespace SpeedClaim.Api.Models;

public class LifePolicy : Policy
{
    public string NomineeName { get; set; }
    public string NomineeRelation { get; set; }
    public string NomineePhone { get; set; }
    public bool HasAccidentalRider { get; set; }
}
