using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<Document?> GetByIdWithUserAsync(Guid documentId);
}
