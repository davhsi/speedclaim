using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Dtos.Sales;

namespace SpeedClaim.Api.Interfaces;

public interface IProposalService
{
    // Customer / Agent
    Task<GenerateQuoteResponse> GenerateQuoteAsync(GenerateQuoteRequest request);
    Task<ProposalDto> SubmitProposalAsync(string userId, SubmitProposalRequest request, bool isAgent);
    Task<IEnumerable<ProposalDto>> GetMyProposalsAsync(string userId, bool isAgent);
    Task<ProposalDto> GetByIdAsync(string proposalId, string userId, bool isAdmin);
    Task<string> UploadDocumentAsync(string proposalId, string uploaderId, string documentType, IFormFile file);

    // Underwriter
    Task<IEnumerable<ProposalDto>> GetAllProposalsAsync();
    Task ApproveOrRejectProposalAsync(string proposalId, string underwriterId, bool isApproved, string notes);
    Task RequestAdditionalDocumentsAsync(string proposalId, string underwriterId, string details);
    Task AddUnderwriterNotesAsync(string proposalId, string underwriterId, string notes);
}
