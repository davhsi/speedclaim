using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class SubmittedDocumentRepository : Repository<SubmittedDocument>, ISubmittedDocumentRepository
{
    public SubmittedDocumentRepository(SpeedClaimDbContext context) : base(context)
    {
    }
}
