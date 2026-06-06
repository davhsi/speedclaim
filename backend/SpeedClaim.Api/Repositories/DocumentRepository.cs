using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdWithUserAsync(Guid documentId)
    {
        return await Context.Documents
            .Include(d => d.User)
            .SingleOrDefaultAsync(d => d.Id == documentId);
    }
}
