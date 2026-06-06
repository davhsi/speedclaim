using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using Asp.Versioning;

namespace SpeedClaim.Api.Controllers;

[Authorize]
[ApiVersion("1.0")]
public class KycController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IEmailService _emailService;

    public KycController(IUnitOfWork unitOfWork, IStorageService storageService, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _emailService = emailService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadKycDocument(IFormFile file, [FromForm] string documentType)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var allowedTypes = new[] { "AADHAAR", "PAN", "PHOTOGRAPH", "ADDRESS_PROOF" };
        if (string.IsNullOrEmpty(documentType) || !allowedTypes.Contains(documentType.ToUpper()))
        {
            return BadRequest($"Invalid documentType. Allowed values: {string.Join(", ", allowedTypes)}");
        }

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        var documentTypeCode = documentType.ToUpper();
        string folderPath = $"kyc/{userId}";
        string fileName = $"{documentTypeCode}_{file.FileName}";
        
        // 1. Check if a Document of this type already exists for this User
        var existingDocument = await _unitOfWork.Documents
            .SingleOrDefaultAsync(d => d.UserId == userId && d.DocumentTypeCode == documentTypeCode && d.Domain == "AUTH");

        // 2. If it exists, physically delete the old file to save space
        if (existingDocument != null)
        {
            await _storageService.DeleteFileAsync(existingDocument.FilePath);
        }

        // 3. Upload the new file
        string fileId;
        using (var stream = file.OpenReadStream())
        {
            fileId = await _storageService.UploadFileAsync(stream, fileName, folderPath);
        }

        // 4. Save/Update Document tracking in the database
        Guid returnedDocumentId;
        if (existingDocument != null)
        {
            existingDocument.FilePath = fileId;
            existingDocument.FileName = file.FileName;
            existingDocument.UploadedAt = DateTime.UtcNow;
            existingDocument.VerificationStatus = "PENDING";
            _unitOfWork.Documents.Update(existingDocument);
            returnedDocumentId = existingDocument.Id;
        }
        else
        {
            var newDocument = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Domain = "AUTH",
                DocumentTypeCode = documentTypeCode,
                FileName = file.FileName,
                FilePath = fileId,
                VerificationStatus = "PENDING",
                UploadedAt = DateTime.UtcNow
            };
            await _unitOfWork.Documents.AddAsync(newDocument);
            returnedDocumentId = newDocument.Id;
        }

        // Keep as pending, Admin/Agent must verify it
        if (user.KycStatus == "NONE" || user.KycStatus == "REJECTED")
        {
            user.KycStatus = "PENDING";
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
        }
        else 
        {
            await _unitOfWork.CompleteAsync();
        }

        return Ok(new 
        { 
            message = "Document uploaded successfully", 
            documentId = returnedDocumentId,
            documentType = documentType.ToUpper(),
            fileId = fileId,
            kycStatus = user.KycStatus
        });
    }

    [HttpPut("documents/{documentId}/verify")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> VerifyDocument(Guid documentId, [FromBody] VerifyDocumentRequest request)
    {
        var document = await _unitOfWork.Documents
            .GetByIdWithUserAsync(documentId);

        if (document == null)
        {
            return NotFound("Document not found.");
        }

        var validStatuses = new[] { "VERIFIED", "REJECTED" };
        if (!validStatuses.Contains(request.Status.ToUpper()))
        {
            return BadRequest("Status must be VERIFIED or REJECTED.");
        }

        // 1. Update the document's verification status
        document.VerificationStatus = request.Status.ToUpper();
        
        var reviewerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(reviewerIdStr, out var reviewerId))
        {
            document.ReviewedById = reviewerId;
        }

        if (document.VerificationStatus == "REJECTED")
        {
            document.RejectionReason = request.Reason;
        }
        else
        {
            document.RejectionReason = null; // Clear if it was previously rejected and now verified
        }

        _unitOfWork.Documents.Update(document);

        // 2. Fetch all current documents for the user to evaluate overall KYC
        var allUserDocs = await _unitOfWork.Documents
            .FindAsync(d => d.UserId == document.UserId && d.Domain == "AUTH");

        // 3. Business Rule: User is VERIFIED only if both AADHAAR and PAN are verified.
        // We evaluate this in-memory based on the potentially updated list.
        // Note: We use the just-updated 'document' status directly or let EF handle the state if it's already in 'allUserDocs'
        var activeDocs = allUserDocs.Where(d => d.Id != document.Id).ToList();
        activeDocs.Add(document); // ensure we evaluate with the latest status

        bool hasVerifiedAadhaar = activeDocs.Any(d => d.DocumentTypeCode == "AADHAAR" && d.VerificationStatus == "VERIFIED");
        bool hasVerifiedPan = activeDocs.Any(d => d.DocumentTypeCode == "PAN" && d.VerificationStatus == "VERIFIED");

        var user = document.User;
        if (hasVerifiedAadhaar && hasVerifiedPan)
        {
            if (user.KycStatus != "VERIFIED")
            {
                user.KycStatus = "VERIFIED";
                var subject = "SpeedClaim KYC Verified";
                var body = $"<h1>Congratulations {user.FullName}!</h1><p>Your KYC documents have been successfully verified. You now have full access to purchase policies.</p>";
                await _emailService.SendEmailAsync(user.Email, user.FullName, subject, body);
            }
        }
        else if (user.KycStatus == "VERIFIED") 
        {
            // If they were verified but a document just got rejected, downgrade them back to PENDING.
            user.KycStatus = "PENDING";
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.CompleteAsync();

        return Ok(new 
        { 
            message = $"Document {document.DocumentTypeCode} is now {document.VerificationStatus}",
            userKycStatus = user.KycStatus 
        });
    }
}

public record VerifyDocumentRequest(string Status, string? Reason = null);
