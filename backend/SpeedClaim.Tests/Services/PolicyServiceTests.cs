using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class PolicyServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IStorageService> _mockStorageService;
    private PolicyService _policyService;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>() { DefaultValue = DefaultValue.Mock };
        _mockStorageService = new Mock<IStorageService>();

        _policyService = new PolicyService(_mockUnitOfWork.Object, _mockStorageService.Object);
    }

    [Test]
    public async Task GetMyPoliciesAsync_ReturnsMappedPolicies()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var policies = new List<Policy>
        {
            new Policy { Id = Guid.NewGuid(), CustomerId = customerId, PolicyNumber = "POL-1" },
            new Policy { Id = Guid.NewGuid(), CustomerId = customerId, PolicyNumber = "POL-2" }
        };

        _mockUnitOfWork.Setup(u => u.Policies.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(policies);

        // Act
        var result = await _policyService.GetMyPoliciesAsync(customerId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().PolicyNumber, Is.EqualTo("POL-1"));
    }

    [Test]
    public void DownloadPolicyDocumentAsync_PolicyNotFound_ThrowsException()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync((Policy)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _policyService.DownloadPolicyDocumentAsync(policyId, customerId));
    }

    [Test]
    public async Task DownloadPolicyDocumentAsync_ValidPolicy_ReturnsGeneratedDocument()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, PolicyNumber = "POL-TEST-001" };

        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var result = await _policyService.DownloadPolicyDocumentAsync(policyId, customerId);

        Assert.That(result, Is.Not.Empty);
        var text = System.Text.Encoding.UTF8.GetString(result);
        Assert.That(text, Does.Contain("POLICY DOCUMENT"));
        Assert.That(text, Does.Contain("POL-TEST-001"));
    }

    [Test]
    public async Task DownloadPolicyDocumentAsync_Success_ReturnsByteArray()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId };

        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var result = await _policyService.DownloadPolicyDocumentAsync(policyId, customerId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task RequestEndorsementAsync_Success_AddsEndorsement()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId };
        var request = new RequestEndorsementRequest(EndorsementType.AddressChange, "Moving", null, null);

        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        await _policyService.RequestEndorsementAsync(policyId, customerId, request);

        _mockUnitOfWork.Verify(u => u.Endorsements.AddAsync(It.Is<Endorsement>(e => e.PolicyId == policyId && e.EndorsementType == EndorsementType.AddressChange)), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ApproveRejectEndorsementAsync_InvalidStatus_ThrowsException()
    {
        var endorsementId = Guid.NewGuid();
        var underwriterId = Guid.NewGuid();
        var endorsement = new Endorsement { Id = endorsementId, Status = EndorsementStatus.Approved };

        _mockUnitOfWork.Setup(u => u.Endorsements.GetByIdAsync(endorsementId)).ReturnsAsync(endorsement);

        Assert.ThrowsAsync<InvalidOperationException>(() => 
            _policyService.ApproveRejectEndorsementAsync(endorsementId, true, "OK", underwriterId));
    }

    [Test]
    public async Task ApproveRejectEndorsementAsync_Success_UpdatesStatus()
    {
        var endorsementId = Guid.NewGuid();
        var underwriterId = Guid.NewGuid();
        var endorsement = new Endorsement { Id = endorsementId, Status = EndorsementStatus.Requested };

        _mockUnitOfWork.Setup(u => u.Endorsements.GetByIdAsync(endorsementId)).ReturnsAsync(endorsement);

        await _policyService.ApproveRejectEndorsementAsync(endorsementId, true, "OK", underwriterId);

        Assert.That(endorsement.Status, Is.EqualTo(EndorsementStatus.Approved));
        Assert.That(endorsement.ReviewedById, Is.EqualTo(underwriterId));
        _mockUnitOfWork.Verify(u => u.Endorsements.Update(endorsement), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    // --- UpdateNomineeAsync tests ---

    [Test]
    public void UpdateNomineeAsync_NomineeNotFound_ThrowsKeyNotFound()
    {
        var nomineeId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockUnitOfWork.Setup(u => u.Nominees.GetByIdAsync(nomineeId)).ReturnsAsync((Nominee?)null);

        var request = new UpdateNomineeRequest("Jane Doe", "Spouse", DateTime.UtcNow.AddYears(-30), 100, false, null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _policyService.UpdateNomineeAsync(nomineeId, customerId, request));
    }

    [Test]
    public void UpdateNomineeAsync_WrongCustomer_ThrowsUnauthorized()
    {
        var nomineeId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();

        var nominee = new Nominee { Id = nomineeId, PolicyId = policyId };
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid() }; // Different customer

        _mockUnitOfWork.Setup(u => u.Nominees.GetByIdAsync(nomineeId)).ReturnsAsync(nominee);
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var request = new UpdateNomineeRequest("Jane", "Spouse", DateTime.UtcNow.AddYears(-25), 100, false, null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _policyService.UpdateNomineeAsync(nomineeId, customerId, request));
    }

    [Test]
    public async Task UpdateNomineeAsync_ValidRequest_UpdatesNomineeFields()
    {
        var nomineeId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var dob = DateTime.UtcNow.AddYears(-28);

        var nominee = new Nominee { Id = nomineeId, PolicyId = policyId, FullName = "Old Name" };
        var policy = new Policy { Id = policyId, CustomerId = customerId };

        _mockUnitOfWork.Setup(u => u.Nominees.GetByIdAsync(nomineeId)).ReturnsAsync(nominee);
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

        var request = new UpdateNomineeRequest("Jane Doe", "Spouse", dob, 100m, false, null);

        await _policyService.UpdateNomineeAsync(nomineeId, customerId, request);

        Assert.That(nominee.FullName, Is.EqualTo("Jane Doe"));
        Assert.That(nominee.Relationship, Is.EqualTo("Spouse"));
        Assert.That(nominee.DateOfBirth, Is.EqualTo(dob));
        Assert.That(nominee.SharePercentage, Is.EqualTo(100m));
        Assert.That(nominee.UpdatedAt, Is.Not.Null);
        _mockUnitOfWork.Verify(u => u.Nominees.Update(nominee), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetByIdAsync_NullCustomer_ReturnsPolicy()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, PolicyNumber = "POL-001", CustomerId = Guid.NewGuid(), Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var result = await _policyService.GetByIdAsync(policyId);

        Assert.That(result.Id, Is.EqualTo(policyId));
    }

    [Test]
    public void GetByIdAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _policyService.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public void GetByIdAsync_WrongCustomer_ThrowsUnauthorized()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid(), Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _policyService.GetByIdAsync(policyId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetNomineesAsync_PolicyWithProposal_ReturnsNominees()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var proposalId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, ProposalId = proposalId, Status = PolicyStatus.Active };
        var nominees = new List<Nominee>
        {
            new Nominee { Id = Guid.NewGuid(), ProposalId = proposalId, FullName = "Jane Doe", Relationship = "Spouse", DateOfBirth = new DateTime(1990, 1, 1), SharePercentage = 100 }
        };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Nominees.FindAsync(It.IsAny<Expression<Func<Nominee, bool>>>())).ReturnsAsync(nominees);

        var result = await _policyService.GetNomineesAsync(policyId, customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().FullName, Is.EqualTo("Jane Doe"));
    }

    [Test]
    public void GetNomineesAsync_WrongCustomer_ThrowsUnauthorized()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid(), Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _policyService.GetNomineesAsync(policyId, Guid.NewGuid()));
    }

    [Test]
    public async Task CancelPolicyAsync_ActivePolicy_SetsCancelled()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        await _policyService.CancelPolicyAsync(policyId, customerId);

        Assert.That(policy.Status, Is.EqualTo(PolicyStatus.Cancelled));
        _mockUnitOfWork.Verify(u => u.PolicyStatusHistories.AddAsync(It.Is<PolicyStatusHistory>(h =>
            h.OldStatus == PolicyStatus.Active && h.NewStatus == PolicyStatus.Cancelled)), Times.Once);
    }

    [Test]
    public void CancelPolicyAsync_AlreadyCancelled_ThrowsInvalidOperation()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Cancelled };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<InvalidOperationException>(() => _policyService.CancelPolicyAsync(policyId, customerId));
    }

    [Test]
    public void CancelPolicyAsync_WrongCustomer_ThrowsKeyNotFound()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid(), Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _policyService.CancelPolicyAsync(policyId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetPolicyHistoryAsync_WithCustomerId_VerifiesOwnershipAndReturnsHistory()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Active };
        var history = new List<PolicyStatusHistory>
        {
            new PolicyStatusHistory { Id = Guid.NewGuid(), PolicyId = policyId, OldStatus = PolicyStatus.Pending, NewStatus = PolicyStatus.Active, ChangedAt = DateTimeOffset.UtcNow }
        };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.PolicyStatusHistories.FindAsync(It.IsAny<Expression<Func<PolicyStatusHistory, bool>>>())).ReturnsAsync(history);

        var result = await _policyService.GetPolicyHistoryAsync(policyId, customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public void GetPolicyHistoryAsync_WrongCustomer_ThrowsUnauthorized()
    {
        var policyId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid(), Status = PolicyStatus.Active };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _policyService.GetPolicyHistoryAsync(policyId, Guid.NewGuid()));
    }

    [Test]
    public async Task GetPolicyEndorsementsAsync_ValidPolicy_ReturnsEndorsements()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Active };
        var endorsements = new List<Endorsement>
        {
            new Endorsement { Id = Guid.NewGuid(), PolicyId = policyId, EndorsementType = EndorsementType.AddressChange, Description = "New address", Status = EndorsementStatus.Requested }
        };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);
        _mockUnitOfWork.Setup(u => u.Endorsements.FindAsync(It.IsAny<Expression<Func<Endorsement, bool>>>())).ReturnsAsync(endorsements);

        var result = await _policyService.GetPolicyEndorsementsAsync(policyId, customerId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetPendingEndorsementsAsync_ReturnsOnlyRequested()
    {
        var endorsements = new List<Endorsement>
        {
            new Endorsement { Id = Guid.NewGuid(), PolicyId = Guid.NewGuid(), EndorsementType = EndorsementType.NomineeChange, Description = "Change nominee", Status = EndorsementStatus.Requested }
        };
        _mockUnitOfWork.Setup(u => u.Endorsements.FindAsync(It.IsAny<Expression<Func<Endorsement, bool>>>())).ReturnsAsync(endorsements);

        var result = await _policyService.GetPendingEndorsementsAsync();

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetNomineesAsync_NoPolicyProposalId_ReturnsEmpty()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, ProposalId = null };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var result = await _policyService.GetNomineesAsync(policyId, customerId);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CancelPolicyAsync_PolicyNotActiveOrPending_ThrowsInvalidOperation()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = customerId, Status = PolicyStatus.Expired };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<InvalidOperationException>(() => _policyService.CancelPolicyAsync(policyId, customerId));
    }

    [Test]
    public async Task DownloadPolicyDocumentAsync_WithIssuedAt_IncludesIssuedAtLine()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy
        {
            Id = policyId, CustomerId = customerId, PolicyNumber = "POL-1",
            Status = PolicyStatus.Active, PolicyType = PolicyType.Individual,
            SumAssured = 500000, PremiumAmount = 12000,
            PaymentFrequency = "Annual",
            StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2026, 1, 1),
            IssuedAt = DateTimeOffset.UtcNow
        };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        var result = await _policyService.DownloadPolicyDocumentAsync(policyId, customerId);

        var text = System.Text.Encoding.UTF8.GetString(result);
        Assert.That(text, Does.Contain("Issued At"));
    }

    [Test]
    public void RequestEndorsementAsync_PolicyNotFound_ThrowsKeyNotFound()
    {
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Policy?)null);
        var request = new RequestEndorsementRequest(EndorsementType.ContactUpdate, "Update contact", "old phone", "new phone");

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _policyService.RequestEndorsementAsync(Guid.NewGuid(), Guid.NewGuid(), request));
    }

    [Test]
    public async Task GetAssignedCustomerPoliciesAsync_ReturnsAgentPolicies()
    {
        var agentId = Guid.NewGuid();
        var policies = new List<Policy>
        {
            new Policy { Id = Guid.NewGuid(), AgentId = agentId, PolicyNumber = "POL-A1" }
        };
        _mockUnitOfWork.Setup(u => u.Policies.FindAsync(It.IsAny<Expression<Func<Policy, bool>>>())).ReturnsAsync(policies);

        var result = await _policyService.GetAssignedCustomerPoliciesAsync(agentId);

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllPoliciesAsync_ReturnsAll()
    {
        var policies = new List<Policy>
        {
            new Policy { Id = Guid.NewGuid(), PolicyNumber = "POL-1" },
            new Policy { Id = Guid.NewGuid(), PolicyNumber = "POL-2" }
        };
        _mockUnitOfWork.Setup(u => u.Policies.GetAllAsync()).ReturnsAsync(policies);

        var result = await _policyService.GetAllPoliciesAsync();

        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public void ApproveRejectEndorsementAsync_NotFound_ThrowsKeyNotFound()
    {
        _mockUnitOfWork.Setup(u => u.Endorsements.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Endorsement?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _policyService.ApproveRejectEndorsementAsync(Guid.NewGuid(), true, "approved", Guid.NewGuid()));
    }

    [Test]
    public void GetPolicyEndorsementsAsync_AccessDenied_ThrowsUnauthorized()
    {
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var policy = new Policy { Id = policyId, CustomerId = Guid.NewGuid() };
        _mockUnitOfWork.Setup(u => u.Policies.GetByIdAsync(policyId)).ReturnsAsync(policy);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _policyService.GetPolicyEndorsementsAsync(policyId, customerId));
    }
}
