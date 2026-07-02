using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SpeedClaim.Api.Dtos.Sales;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class ProposalService : IProposalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notifications;
    private readonly IStorageService _storageService;
    private readonly ILogger<ProposalService> _logger;
    private readonly IEmailService _emailService;

    public ProposalService(IUnitOfWork unitOfWork, INotificationService notifications, IStorageService storageService, ILogger<ProposalService> logger, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _storageService = storageService;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<GenerateQuoteResponse> GenerateQuoteAsync(GenerateQuoteRequest request)
    {
        var productId = Guid.Parse(request.ProductId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(productId);
        if (product == null) throw new NotFoundException("Product not found");
        if (!product.IsActive) throw new ConflictException("Product is not available for new quotes.");
        ValidateProductEligibility(product, request.Age, request.SumAssured, request.TenureYears);

        var premiumAmount = await CalculatePremiumAsync(productId, request.Age, request.SumAssured);

        return new GenerateQuoteResponse(premiumAmount, request.SumAssured, request.TenureYears, "Monthly");
    }

    private static void ValidateProductEligibility(InsuranceProduct product, int age, decimal sumAssured, int tenureYears)
    {
        if (age < product.MinAge || age > product.MaxAge)
            throw new ValidationException($"Age must be between {product.MinAge} and {product.MaxAge} for this product.");

        if (sumAssured < product.MinSumAssured || sumAssured > product.MaxSumAssured)
            throw new ValidationException($"Sum assured must be between {product.MinSumAssured} and {product.MaxSumAssured}.");

        if (tenureYears < product.MinTenureYears || tenureYears > product.MaxTenureYears)
            throw new ValidationException($"Tenure must be between {product.MinTenureYears} and {product.MaxTenureYears} years.");
    }

    private async Task<decimal> CalculatePremiumAsync(Guid productId, int age, decimal sumAssured)
    {
        var rateTables = await _unitOfWork.PremiumRateTables.FindAsync(r => r.ProductId == productId);
        var applicableRate = rateTables.FirstOrDefault(r =>
            r.AgeMin <= age && r.AgeMax >= age &&
            r.SumAssuredMin <= sumAssured && r.SumAssuredMax >= sumAssured);

        if (applicableRate == null)
            throw new NotFoundException("No applicable rate found for the given criteria");

        return applicableRate.AnnualPremium;
    }

    public async Task<ProposalDto> SubmitProposalAsync(string userId, SubmitProposalRequest request, bool isAgent)
    {
        var uId = Guid.Parse(userId);
        var customerId = Guid.Parse(request.CustomerId);
        var productId = Guid.Parse(request.ProductId);

        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(productId);
        if (product == null) throw new NotFoundException("Product not found");
        if (!product.IsActive) throw new ConflictException("Product is not available for new proposals.");

        var customerRecord = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customerRecord == null) throw new NotFoundException("Customer not found");
        if (!isAgent && customerRecord.UserId != uId)
            throw new ForbiddenException("You can only submit proposals for your own customer profile.");

        Agent? agent = null;
        if (isAgent)
        {
            agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
            if (agent == null)
                throw new NotFoundException("Agent not found.");

            var assignedProposal = await _unitOfWork.Proposals.FirstOrDefaultAsync(p =>
                p.AgentId == agent.Id && p.CustomerId == customerId);
            if (assignedProposal == null)
                throw new ForbiddenException("Customer is not assigned to this agent.");
        }

        if (!customerRecord.DateOfBirth.HasValue)
            throw new ValidationException("Customer date of birth is required before submitting a proposal.");

        var customerAge = CalculateAge(customerRecord.DateOfBirth.Value);
        ValidateProductEligibility(product, customerAge, request.SumAssured, request.TenureYears);

        var kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == customerRecord.UserId);
        if (kyc == null || kyc.KycStatus != KycStatus.Approved)
            throw new ForbiddenException("KYC must be approved before submitting a proposal.");

        var premiumAmount = await CalculatePremiumAsync(productId, customerAge, request.SumAssured);

        var proposal = new Proposal
        {
            ProposalNumber = $"PRP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            CustomerId = customerId,
            ProductId = productId,
            PolicyType = request.CustomerMemberIds != null && request.CustomerMemberIds.Count > 0 ? PolicyType.FamilyFloater : PolicyType.Individual,
            SumAssured = request.SumAssured,
            TenureYears = request.TenureYears,
            PremiumAmount = premiumAmount,
            PaymentFrequency = request.PaymentFrequency,
            Status = ProposalStatus.Submitted,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (agent != null)
        {
            proposal.AgentId = agent.Id;
        }

        if (request.HealthDetail != null)
        {
            proposal.HealthDetail = new HealthDetail
            {
                PreExistingConditions = request.HealthDetail.PreExistingConditions,
                NetworkHospitalCoverage = request.HealthDetail.NetworkHospitalCoverage,
                TpaName = request.HealthDetail.TpaName,
                RoomRentLimit = request.HealthDetail.RoomRentLimit,
                MaternityCovered = request.HealthDetail.MaternityCovered,
                CopayPercentage = request.HealthDetail.CopayPercentage,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        if (request.LifeDetail != null)
        {
            proposal.LifeDetail = new LifeDetail
            {
                PolicySubtype = request.LifeDetail.PolicySubtype,
                MaturityBenefit = request.LifeDetail.MaturityBenefit,
                DeathBenefit = request.LifeDetail.DeathBenefit,
                SurrenderValueApplicable = request.LifeDetail.SurrenderValueApplicable,
                LoanEligible = request.LifeDetail.LoanEligible,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        if (request.MotorDetail != null)
        {
            proposal.MotorDetail = new MotorDetail
            {
                VehicleNumber = request.MotorDetail.VehicleNumber,
                VehicleMake = request.MotorDetail.VehicleMake,
                VehicleModel = request.MotorDetail.VehicleModel,
                ManufactureYear = request.MotorDetail.ManufactureYear,
                VehicleType = request.MotorDetail.VehicleType,
                Idv = request.MotorDetail.Idv,
                EngineNumber = request.MotorDetail.EngineNumber,
                ChassisNumber = request.MotorDetail.ChassisNumber,
                CoverType = request.MotorDetail.CoverType,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        if (request.CustomerMemberIds != null)
        {
            foreach (var cmId in request.CustomerMemberIds)
            {
                proposal.ProposalMembers.Add(new ProposalMember
                {
                    CustomerMemberId = Guid.Parse(cmId),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (request.Nominees != null)
        {
            foreach (var n in request.Nominees)
            {
                proposal.Nominees.Add(new Nominee
                {
                    FullName = n.FullName,
                    Relationship = n.Relationship,
                    DateOfBirth = n.DateOfBirth,
                    SharePercentage = n.SharePercentage,
                    IsMinor = n.IsMinor,
                    AppointeeName = n.AppointeeName,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await _unitOfWork.Proposals.AddAsync(proposal);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Proposal submitted: {ProposalNumber} for Customer {CustomerId}, Product {ProductId}", proposal.ProposalNumber, customerId, productId);

        var proposalUser = await _unitOfWork.Users.GetByIdAsync(customerRecord.UserId);
        if (proposalUser != null)
            await _emailService.SendTemplatedEmailAsync("ProposalSubmitted", new Dictionary<string, string>
            {
                ["firstName"]      = WebUtility.HtmlEncode(proposalUser.FirstName),
                ["proposalNumber"] = WebUtility.HtmlEncode(proposal.ProposalNumber)
            }, proposalUser.Email);

        return new ProposalDto(
            proposal.Id,
            proposal.ProposalNumber,
            proposal.CustomerId,
            proposal.AgentId,
            proposal.ProductId,
            proposal.Status.ToString(),
            proposal.SumAssured,
            proposal.TenureYears,
            proposal.PremiumAmount,
            proposal.PaymentFrequency,
            proposal.CreatedAt,
            product.ProductName
        );
    }

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    public async Task<IEnumerable<ProposalDto>> GetMyProposalsAsync(string userId, bool isAgent)
    {
        var uId = Guid.Parse(userId);
        IEnumerable<Proposal> proposals;

        if (isAgent)
        {
            var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
            if (agent == null) return new List<ProposalDto>();
            proposals = await _unitOfWork.Proposals.FindAsync(p => p.AgentId == agent.Id);
        }
        else
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uId);
            if (customer == null) return new List<ProposalDto>();
            proposals = await _unitOfWork.Proposals.FindAsync(p => p.CustomerId == customer.Id);
        }

        return proposals.Select(p => new ProposalDto(
            p.Id,
            p.ProposalNumber,
            p.CustomerId,
            p.AgentId,
            p.ProductId,
            p.Status.ToString(),
            p.SumAssured,
            p.TenureYears,
            p.PremiumAmount,
            p.PaymentFrequency,
            p.CreatedAt,
            p.Product?.ProductName
        ));
    }

    public async Task<IEnumerable<ProposalDto>> GetAllProposalsAsync()
    {
        var proposals = await _unitOfWork.Proposals.GetAllAsync();
        return proposals.Select(p => new ProposalDto(
            p.Id,
            p.ProposalNumber,
            p.CustomerId,
            p.AgentId,
            p.ProductId,
            p.Status.ToString(),
            p.SumAssured,
            p.TenureYears,
            p.PremiumAmount,
            p.PaymentFrequency,
            p.CreatedAt,
            p.Product?.ProductName
        ));
    }

    public async Task<ProposalDto> GetByIdAsync(string proposalId, string userId, bool isAdmin)
    {
        var pId = Guid.Parse(proposalId);
        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found.");

        if (!isAdmin)
        {
            var uId = Guid.Parse(userId);
            var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uId);
            bool isOwner = (customer != null && proposal.CustomerId == customer.Id)
                || (agent != null && proposal.AgentId == agent.Id);
            if (!isOwner) throw new ForbiddenException("Access denied to this proposal.");
        }

        var proposalMembers = await _unitOfWork.ProposalMembers.FindAsync(m => m.ProposalId == pId);
        var memberDtos = new List<ProposalMemberDto>();
        foreach (var member in proposalMembers)
        {
            var customerMember = await _unitOfWork.CustomerMembers.GetByIdAsync(member.CustomerMemberId);
            if (customerMember != null)
                memberDtos.Add(new ProposalMemberDto(member.Id, member.CustomerMemberId, customerMember.FullName, customerMember.Relationship.ToString()));
        }

        var nominees = (await _unitOfWork.Nominees.FindAsync(n => n.ProposalId == pId))
            .Select(n => new ProposalNomineeDto(n.Id, n.FullName, n.Relationship, n.SharePercentage, n.DateOfBirth, n.IsMinor, n.AppointeeName))
            .ToList();

        var documents = (await _unitOfWork.SubmittedDocuments.FindAsync(d => d.EntityType == EntityType.Proposal && d.EntityId == pId))
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new SpeedClaim.Api.Dtos.Claims.SubmittedDocumentDto(
                d.Id,
                d.DocumentKey,
                string.IsNullOrWhiteSpace(d.OriginalFilename) ? d.DocumentKey : d.OriginalFilename,
                d.FilePath,
                d.UploadedAt))
            .ToList();

        return new ProposalDto(
            proposal.Id,
            proposal.ProposalNumber,
            proposal.CustomerId,
            proposal.AgentId,
            proposal.ProductId,
            proposal.Status.ToString(),
            proposal.SumAssured,
            proposal.TenureYears,
            proposal.PremiumAmount,
            proposal.PaymentFrequency,
            proposal.CreatedAt,
            proposal.Product?.ProductName,
            proposal.Product?.Domain,
            proposal.UnderwriterNotes,
            memberDtos,
            nominees,
            documents
        );
    }

    public async Task<string> UploadDocumentAsync(string proposalId, string uploaderId, string documentType, IFormFile file)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(uploaderId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found.");

        var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uId);
        bool canUpload = (customer != null && proposal.CustomerId == customer.Id)
            || (agent != null && proposal.AgentId == agent.Id);
        if (!canUpload)
            throw new ForbiddenException("Access denied to this proposal.");

        if (file == null || file.Length == 0) throw new ValidationException("Invalid file.");

        var existing = await _unitOfWork.SubmittedDocuments.FindAsync(
            d => d.EntityId == pId && d.DocumentKey == documentType);
        foreach (var old in existing)
            _unitOfWork.SubmittedDocuments.Delete(old);

        using var stream = file.OpenReadStream();
        var storedPath = await _storageService.UploadFileAsync(stream, file.FileName, $"proposals/{pId}");

        await _unitOfWork.SubmittedDocuments.AddAsync(new SubmittedDocument
        {
            Id = Guid.NewGuid(),
            EntityType = EntityType.Proposal,
            EntityId = pId,
            DocumentKey = documentType,
            OriginalFilename = file.FileName,
            StoredFilename = $"{Guid.NewGuid()}_{file.FileName}",
            FilePath = storedPath,
            UploadedBy = uId,
            UploadedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();
        return storedPath;
    }

    public async Task ApproveOrRejectProposalAsync(string proposalId, string underwriterId, bool isApproved, string notes)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(underwriterId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Status is ProposalStatus.Approved or ProposalStatus.Rejected)
            throw new ConflictException($"Proposal has already been {proposal.Status}.");
        if (proposal.Status is not (ProposalStatus.Submitted or ProposalStatus.UnderReview))
            throw new UnprocessableException("Only submitted or under-review proposals can receive a final decision.");

        proposal.Status = isApproved ? ProposalStatus.Approved : ProposalStatus.Rejected;
        proposal.UnderwriterId = uId;
        proposal.ReviewedAt = DateTimeOffset.UtcNow;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        Policy? issuedPolicy = null;
        if (isApproved)
        {
            proposal.UnderwriterNotes = notes;

            var activationDate = DateTime.UtcNow.AddDays(7);
            issuedPolicy = new Policy
            {
                Id = Guid.NewGuid(),
                PolicyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                ProposalId = proposal.Id,
                CustomerId = proposal.CustomerId,
                ProductId = proposal.ProductId,
                AgentId = proposal.AgentId,
                PolicyType = proposal.PolicyType,
                SumAssured = proposal.SumAssured,
                PremiumAmount = proposal.PremiumAmount,
                PaymentFrequency = proposal.PaymentFrequency,
                StartDate = activationDate,
                EndDate = activationDate.AddYears(proposal.TenureYears),
                Status = PolicyStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.Policies.AddAsync(issuedPolicy);

            await _unitOfWork.PremiumSchedules.AddAsync(new PremiumSchedule
            {
                Id = Guid.NewGuid(),
                ProposalId = proposal.Id,
                PolicyId = issuedPolicy.Id,
                InstallmentNumber = 1,
                DueDate = activationDate,
                Amount = proposal.PremiumAmount,
                Status = PremiumScheduleStatus.Upcoming,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            proposal.RejectionReason = notes;
        }

        _unitOfWork.Proposals.Update(proposal);
        await _unitOfWork.AuditLogs.AddAsync(new Models.AuditLog
        {
            Id = Guid.NewGuid(), UserId = uId, EntityType = "Proposal", EntityId = pId,
            Action = isApproved ? "ProposalApproved" : "ProposalRejected",
            NewValue = JsonSerializer.Serialize(notes), CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Proposal {ProposalNumber} {Decision} by Underwriter {UnderwriterId}", proposal.ProposalNumber, isApproved ? "approved" : "rejected", underwriterId);

        // Notify customer
        var customer = await _unitOfWork.Customers.GetByIdAsync(proposal.CustomerId);
        if (customer != null)
        {
            var (title, message) = isApproved
                ? ("Proposal Approved", $"Your proposal {proposal.ProposalNumber} has been approved. Please make your first payment to activate the policy.")
                : ("Proposal Rejected", $"Your proposal {proposal.ProposalNumber} has been rejected. Reason: {notes}");
            await _notifications.CreateAsync(customer.UserId, title, message, "policy");
        }

        // Notify agent if one was assigned
        if (proposal.AgentId.HasValue)
        {
            var agent = await _unitOfWork.Agents.GetByIdAsync(proposal.AgentId.Value);
            if (agent != null)
            {
                var agentMsg = isApproved
                    ? $"Proposal {proposal.ProposalNumber} for your customer has been approved."
                    : $"Proposal {proposal.ProposalNumber} for your customer has been rejected.";
                await _notifications.CreateAsync(agent.UserId, isApproved ? "Proposal Approved" : "Proposal Rejected", agentMsg, "policy");
            }
        }

        // Email customer
        if (customer != null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
            if (user != null)
            {
                var templateKey = isApproved ? "ProposalApproved" : "ProposalRejected";
                var vars = new Dictionary<string, string>
                {
                    ["firstName"]      = WebUtility.HtmlEncode(user.FirstName),
                    ["proposalNumber"] = WebUtility.HtmlEncode(proposal.ProposalNumber)
                };
                if (!isApproved && !string.IsNullOrWhiteSpace(notes))
                    vars["rejectionReason"] = WebUtility.HtmlEncode(notes);
                await _emailService.SendTemplatedEmailAsync(templateKey, vars, user.Email);

                if (isApproved && issuedPolicy != null)
                    await _emailService.SendTemplatedEmailAsync("PolicyIssued", new Dictionary<string, string>
                    {
                        ["firstName"]      = WebUtility.HtmlEncode(user.FirstName),
                        ["proposalNumber"] = WebUtility.HtmlEncode(proposal.ProposalNumber),
                        ["policyNumber"]   = WebUtility.HtmlEncode(issuedPolicy.PolicyNumber)
                    }, user.Email);
            }
        }
    }

    public async Task RequestAdditionalDocumentsAsync(string proposalId, string underwriterId, string details)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(underwriterId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found");
        if (proposal.Status is ProposalStatus.Approved or ProposalStatus.Rejected)
            throw new ConflictException($"Documents cannot be requested for a {proposal.Status} proposal.");

        proposal.Status = ProposalStatus.DocumentsPending;
        proposal.UnderwriterId = uId;
        proposal.UnderwriterNotes = $"Additional Docs Required: {details}";
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();

        var docsCustomer = await _unitOfWork.Customers.GetByIdAsync(proposal.CustomerId);
        if (docsCustomer != null)
        {
            var docsUser = await _unitOfWork.Users.GetByIdAsync(docsCustomer.UserId);
            if (docsUser != null)
                await _emailService.SendTemplatedEmailAsync("ProposalDocumentsPending", new Dictionary<string, string>
                {
                    ["firstName"]      = WebUtility.HtmlEncode(docsUser.FirstName),
                    ["proposalNumber"] = WebUtility.HtmlEncode(proposal.ProposalNumber),
                    ["details"]        = WebUtility.HtmlEncode(details)
                }, docsUser.Email);
        }
    }

    public async Task AddUnderwriterNotesAsync(string proposalId, string underwriterId, string notes)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(underwriterId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found");

        // Append to existing notes if any
        var existing = proposal.UnderwriterNotes;
        proposal.UnderwriterNotes = string.IsNullOrWhiteSpace(existing)
            ? notes
            : $"{existing}\n---\n{notes}";

        proposal.UnderwriterId = uId;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }
}
