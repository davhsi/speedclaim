using System;
using System.Collections.Generic;
using System.Linq;
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

    public ProposalService(IUnitOfWork unitOfWork, INotificationService notifications, IStorageService storageService, ILogger<ProposalService> logger)
    {
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<GenerateQuoteResponse> GenerateQuoteAsync(GenerateQuoteRequest request)
    {
        var productId = Guid.Parse(request.ProductId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(productId);
        if (product == null) throw new NotFoundException("Product not found");

        var rateTables = await _unitOfWork.PremiumRateTables.FindAsync(r => r.ProductId == productId);
        var applicableRate = rateTables.FirstOrDefault(r => 
            r.AgeMin <= request.Age && r.AgeMax >= request.Age &&
            r.SumAssuredMin <= request.SumAssured && r.SumAssuredMax >= request.SumAssured);

        if (applicableRate == null)
            throw new NotFoundException("No applicable rate found for the given criteria");

        // Basic quote math: AnnualPremium
        var premiumAmount = applicableRate.AnnualPremium;

        return new GenerateQuoteResponse(premiumAmount, request.SumAssured, request.TenureYears, "Monthly");
    }

    public async Task<ProposalDto> SubmitProposalAsync(string userId, SubmitProposalRequest request, bool isAgent)
    {
        var uId = Guid.Parse(userId);
        var customerId = Guid.Parse(request.CustomerId);
        var productId = Guid.Parse(request.ProductId);

        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(productId);
        if (product == null) throw new NotFoundException("Product not found");

        var proposal = new Proposal
        {
            ProposalNumber = $"PRP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            CustomerId = customerId,
            ProductId = productId,
            PolicyType = request.CustomerMemberIds != null && request.CustomerMemberIds.Count > 0 ? PolicyType.FamilyFloater : PolicyType.Individual,
            SumAssured = request.SumAssured,
            TenureYears = request.TenureYears,
            PremiumAmount = request.PremiumAmount,
            PaymentFrequency = request.PaymentFrequency,
            Status = ProposalStatus.Submitted,
            SubmittedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (isAgent)
        {
            var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
            if (agent != null)
            {
                proposal.AgentId = agent.Id;
            }
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
            proposal.CreatedAt
        );
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
            p.CreatedAt
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
            p.CreatedAt
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
            bool isOwner = proposal.CustomerId == uId
                || (agent != null && proposal.AgentId == agent.Id);
            if (!isOwner) throw new ForbiddenException("Access denied to this proposal.");
        }

        return new ProposalDto(
            proposal.Id, proposal.ProposalNumber, proposal.CustomerId, proposal.AgentId,
            proposal.ProductId, proposal.Status.ToString(), proposal.SumAssured,
            proposal.TenureYears, proposal.PremiumAmount, proposal.PaymentFrequency, proposal.CreatedAt);
    }

    public async Task<string> UploadDocumentAsync(string proposalId, string uploaderId, string documentType, IFormFile file)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(uploaderId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found.");

        if (proposal.CustomerId != uId)
        {
            var agent = await _unitOfWork.Agents.FirstOrDefaultAsync(a => a.UserId == uId);
            if (agent == null || proposal.AgentId != agent.Id)
                throw new ForbiddenException("Access denied to this proposal.");
        }

        if (file == null || file.Length == 0) throw new ValidationException("Invalid file.");

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

        proposal.Status = isApproved ? ProposalStatus.Approved : ProposalStatus.Rejected;
        proposal.UnderwriterId = uId;
        proposal.ReviewedAt = DateTimeOffset.UtcNow;
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        if (isApproved)
        {
            proposal.UnderwriterNotes = notes;

            var activationDate = DateTime.UtcNow.AddDays(7);
            var policy = new Policy
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
            await _unitOfWork.Policies.AddAsync(policy);

            proposal.PremiumSchedules.Add(new PremiumSchedule
            {
                Id = Guid.NewGuid(),
                PolicyId = policy.Id,
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
    }

    public async Task RequestAdditionalDocumentsAsync(string proposalId, string underwriterId, string details)
    {
        var pId = Guid.Parse(proposalId);
        var uId = Guid.Parse(underwriterId);

        var proposal = await _unitOfWork.Proposals.GetByIdAsync(pId);
        if (proposal == null) throw new NotFoundException("Proposal not found");

        proposal.Status = ProposalStatus.DocumentsPending;
        proposal.UnderwriterId = uId;
        proposal.UnderwriterNotes = $"Additional Docs Required: {details}";
        proposal.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
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

