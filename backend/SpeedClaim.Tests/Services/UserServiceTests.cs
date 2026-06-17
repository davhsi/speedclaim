using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class UserServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IRepository<Customer>> _mockCustomerRepository = null!;
    private Mock<IRepository<KycRecord>> _mockKycRepository = null!;
    private Mock<IRepository<Address>> _mockAddressRepository = null!;
    private Mock<IEncryptionService> _mockEncryptionService = null!;
    private UserService _userService = null!;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockCustomerRepository = new Mock<IRepository<Customer>>();
        _mockKycRepository = new Mock<IRepository<KycRecord>>();

        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Customers).Returns(_mockCustomerRepository.Object);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);
        _mockAddressRepository = new Mock<IRepository<Address>>();
        _mockAddressRepository.Setup(r => r.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Linq.Expressions.Expression<System.Func<SpeedClaim.Api.Models.Address, bool>>>(), It.IsAny<System.Func<System.Linq.IQueryable<SpeedClaim.Api.Models.Address>, System.Linq.IQueryable<SpeedClaim.Api.Models.Address>>>()))
            .ReturnsAsync((new System.Collections.Generic.List<Address>(), 0));
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(_mockAddressRepository.Object);
        _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.AuditLogs).Returns(new Mock<IRepository<AuditLog>>().Object);

        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
        _mockEncryptionService.Setup(e => e.Mask(It.IsAny<string>())).Returns<string>(s => s);

        _userService = new UserService(_mockUnitOfWork.Object, new Mock<IStorageService>().Object, _mockEncryptionService.Object);
    }

    [Test]
    public void GetProfileAsync_UserNotFound_ThrowsException()
    {
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _userService.GetProfileAsync(Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task GetProfileAsync_ValidUser_ReturnsProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", FirstName = "John", LastName = "Doe", Role = UserRole.Customer, Salutation = Salutation.Mr };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _userService.GetProfileAsync(userId.ToString());
        Assert.That(result.Email, Is.EqualTo("test@test.com"));
        Assert.That(result.FullName, Is.EqualTo("John Doe"));
    }

    [Test]
    public void UpdateProfileAsync_UserNotFound_ThrowsException()
    {
        var address = new AddressDto("123 St", null, "City", "State", "12345", "Country");
        var request = new UserDto(Guid.NewGuid(), "test@test.com", "Mr", "Jane", "Doe", "Jane Doe", "0987654321", "Customer", "Single", address, address);
        
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _userService.UpdateProfileAsync(Guid.NewGuid().ToString(), request));
        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task UpdateProfileAsync_ValidUser_Success()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FirstName = "Old", LastName = "Name" };
        var address = new AddressDto("123 St", null, "City", "State", "12345", "Country");
        var request = new UserDto(userId, "test@test.com", "Mr", "Jane", "Doe", "Jane Doe", "0987654321", "Customer", "Single", address, address);

        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        await _userService.UpdateProfileAsync(userId.ToString(), request);

        Assert.That(user.FirstName, Is.EqualTo("Jane"));
        Assert.That(user.LastName, Is.EqualTo("Doe"));
        Assert.That(user.Phone, Is.EqualTo("0987654321"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetAllUsersAsync_ReturnsUserList()
    {
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "test1@test.com", Role = UserRole.Customer },
            new User { Id = Guid.NewGuid(), Email = "test2@test.com", Role = UserRole.Agent }
        };
        _mockUserRepository.Setup(r => r.GetPagedAsync(1, 20, null, null))
            .ReturnsAsync(((IEnumerable<User>)users, users.Count));

        var result = await _userService.GetAllUsersAsync(1, 20);

        var resultList = result.Data.ToList();
        Assert.That(resultList.Count, Is.EqualTo(2));
        Assert.That(resultList[0].Email, Is.EqualTo("test1@test.com"));
        Assert.That(resultList[1].Role, Is.EqualTo("Agent"));
    }

    [Test]
    public void UpdateUserRoleAsync_UserNotFound_ThrowsException()
    {
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _userService.UpdateUserRoleAsync(Guid.NewGuid().ToString(), "Admin"));
        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public void UpdateUserRoleAsync_InvalidRole_ThrowsException()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.Customer };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() => _userService.UpdateUserRoleAsync(userId.ToString(), "InvalidRoleName"));
        Assert.That(ex.Message, Is.EqualTo("Invalid role"));
    }

    [Test]
    public async Task UpdateUserRoleAsync_ValidRole_Success()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Role = UserRole.Customer };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        await _userService.UpdateUserRoleAsync(userId.ToString(), "Underwriter");
        
        Assert.That(user.Role, Is.EqualTo(UserRole.Underwriter));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ApproveRejectKycAsync_KycRecordNotFound_ThrowsException()
    {
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync((KycRecord?)null);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _userService.ApproveRejectKycAsync(Guid.NewGuid().ToString(), true, "", Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("KYC Record not found"));
    }

    [Test]
    public async Task ApproveRejectKycAsync_Approved_Success()
    {
        var customerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var kycRecord = new KycRecord { UserId = customerId, KycStatus = KycStatus.Pending, AadhaarNumber = "ENC_AADHAAR", PanNumber = "ENC_PAN" };

        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync(kycRecord);
        await _userService.ApproveRejectKycAsync(customerId.ToString(), true, "All good", reviewerId.ToString());

        Assert.That(kycRecord.KycStatus, Is.EqualTo(KycStatus.Approved));
        Assert.That(kycRecord.ReviewedById, Is.EqualTo(reviewerId));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ActivateDeactivateUserAsync_UserNotFound_ThrowsException()
    {
        _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() => _userService.ActivateDeactivateUserAsync(Guid.NewGuid().ToString(), false, Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Is.EqualTo("User not found"));
    }

    [Test]
    public async Task ActivateDeactivateUserAsync_ValidRequest_Success()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, IsActive = true };
        
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        await _userService.ActivateDeactivateUserAsync(userId.ToString(), false, Guid.NewGuid().ToString());

        Assert.That(user.IsActive, Is.False);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task GetProfileAsync_CustomerWithRecord_IncludesMaritalStatus()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "c@test.com", FirstName = "C", LastName = "D", Role = UserRole.Customer, Salutation = Salutation.Ms };
        var customer = new Customer { Id = Guid.NewGuid(), UserId = userId, MaritalStatus = MaritalStatus.Married };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);

        var result = await _userService.GetProfileAsync(userId.ToString());

        Assert.That(result.MaritalStatus, Is.EqualTo("Married"));
    }

    [Test]
    public async Task UpdateProfileAsync_CustomerWithRecord_UpdatesMaritalStatus()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "x@test.com", FirstName = "X", LastName = "Y", Role = UserRole.Customer, Salutation = Salutation.Mr };
        var customer = new Customer { Id = Guid.NewGuid(), UserId = userId, MaritalStatus = MaritalStatus.Single };
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);

        var dto = new UserDto(userId, "x@test.com", "Mr", "X", "Y", "X Y", "9999", "Customer", "Married", null, null);
        await _userService.UpdateProfileAsync(userId.ToString(), dto);

        Assert.That(customer.MaritalStatus, Is.EqualTo(MaritalStatus.Married));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UploadAadhaarAsync_WithBackDocument_UploadsBack()
    {
        var customerId = Guid.NewGuid();
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync((KycRecord?)null);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var mockFront = new Mock<IFormFile>();
        mockFront.Setup(f => f.FileName).Returns("front.jpg");
        mockFront.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        var mockBack = new Mock<IFormFile>();
        mockBack.Setup(f => f.FileName).Returns("back.jpg");
        mockBack.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());

        var mockStorage = new Mock<IStorageService>();
        var svc = new UserService(_mockUnitOfWork.Object, mockStorage.Object, _mockEncryptionService.Object);

        await svc.UploadAadhaarAsync(customerId.ToString(), new AadhaarUploadRequest(null, "123456789012", mockFront.Object, mockBack.Object));

        mockStorage.Verify(s => s.UploadFileAsync(It.IsAny<System.IO.Stream>(), "back.jpg", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task GetAllUsersAsync_CustomerWithRecord_IncludesMaritalStatus()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "a@b.com", FirstName = "A", LastName = "B", Role = UserRole.Customer, Salutation = Salutation.Mr };
        var customer = new Customer { Id = Guid.NewGuid(), UserId = userId, MaritalStatus = MaritalStatus.Married };
        _mockUserRepository.Setup(r => r.GetPagedAsync(1, 20, null, null))
            .ReturnsAsync(((IEnumerable<User>)new List<User> { user }, 1));
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>())).ReturnsAsync(customer);

        var result = await _userService.GetAllUsersAsync(1, 20);

        Assert.That(result.Data.First().MaritalStatus, Is.EqualTo("Married"));
    }

    [Test]
    public async Task AddFamilyMemberAsync_ValidRequest_CreatesMember()
    {
        var customerId = Guid.NewGuid();
        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerId });
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        var request = new AddFamilyMemberRequest(Salutation.Mr, "John", "Doe", new DateOnly(1990, 1, 1), Gender.Male, Relationship.Sibling, true);

        var result = await _userService.AddFamilyMemberAsync(customerId.ToString(), request);

        Assert.That(result.FirstName, Is.EqualTo("John"));
        mockMemberRepo.Verify(r => r.AddAsync(It.IsAny<CustomerMember>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateFamilyMemberAsync_ValidMember_UpdatesFields()
    {
        var memberId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var member = new CustomerMember { Id = memberId, CustomerId = customerId, FirstName = "Old", LastName = "Name", DateOfBirth = new DateOnly(1990, 1, 1) };

        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerId });
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        mockMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CustomerMember, bool>>>())).ReturnsAsync(member);
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        var request = new UpdateFamilyMemberRequest(Salutation.Mrs, "New", "Name", new DateOnly(1990, 1, 1), Gender.Female, Relationship.Spouse, true);

        await _userService.UpdateFamilyMemberAsync(memberId.ToString(), customerId.ToString(), request);

        Assert.That(member.FirstName, Is.EqualTo("New"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void UpdateFamilyMemberAsync_MemberNotFound_ThrowsException()
    {
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        mockMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CustomerMember, bool>>>())).ReturnsAsync((CustomerMember?)null);
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        var request = new UpdateFamilyMemberRequest(Salutation.Mr, "X", "Y", new DateOnly(1990, 1, 1), Gender.Male, Relationship.Sibling, false);
        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _userService.UpdateFamilyMemberAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), request));
    }

    [Test]
    public async Task GetFamilyMembersAsync_ValidCustomer_ReturnsMembers()
    {
        var customerId = Guid.NewGuid();
        var members = new List<CustomerMember>
        {
            new CustomerMember { Id = Guid.NewGuid(), CustomerId = customerId, FirstName = "Jane", LastName = "Doe", DateOfBirth = new DateOnly(1992, 6, 15) }
        };
        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerId });
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        mockMemberRepo.Setup(r => r.GetPagedAsync(1, 100, It.IsAny<Expression<Func<CustomerMember, bool>>>(), null))
            .ReturnsAsync(((IEnumerable<CustomerMember>)members, 1));
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        var result = await _userService.GetFamilyMembersAsync(customerId.ToString());

        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task UploadAadhaarAsync_NewRecord_CreatesKycRecord()
    {
        var customerId = Guid.NewGuid();
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync((KycRecord?)null);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("front.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());
        var mockStorage = new Mock<IStorageService>();
        var svc = new UserService(_mockUnitOfWork.Object, mockStorage.Object, _mockEncryptionService.Object);

        await svc.UploadAadhaarAsync(customerId.ToString(), new AadhaarUploadRequest(null, "123456789012", mockFile.Object, null));

        _mockKycRepository.Verify(r => r.AddAsync(It.IsAny<KycRecord>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UploadAadhaarAsync_ExistingRecord_UpdatesAadhaarNumber()
    {
        var customerId = Guid.NewGuid();
        var existing = new KycRecord { Id = Guid.NewGuid(), UserId = customerId, KycStatus = KycStatus.Approved, AadhaarNumber = "OLD" };
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync(existing);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("front.jpg");
        mockFile.Setup(f => f.OpenReadStream()).Returns(new System.IO.MemoryStream());
        var mockStorage = new Mock<IStorageService>();
        var svc = new UserService(_mockUnitOfWork.Object, mockStorage.Object, _mockEncryptionService.Object);

        await svc.UploadAadhaarAsync(customerId.ToString(), new AadhaarUploadRequest(null, "123456789012", mockFile.Object, null));

        Assert.That(existing.AadhaarNumber, Is.EqualTo("123456789012"));
        Assert.That(existing.KycStatus, Is.EqualTo(KycStatus.Pending));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void ApproveRejectKycAsync_MissingAadhaarOrPan_ThrowsValidationException()
    {
        var customerId = Guid.NewGuid();
        var kycRecord = new KycRecord { UserId = customerId, KycStatus = KycStatus.Pending, AadhaarNumber = "ENC_AADHAAR", PanNumber = null };
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync(kycRecord);

        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(() =>
            _userService.ApproveRejectKycAsync(customerId.ToString(), true, "", Guid.NewGuid().ToString()));
        Assert.That(ex.Message, Does.Contain("Aadhaar and PAN"));
    }

    [Test]
    public async Task AddAddressAsync_ValidRequest_CreatesAddress()
    {
        var userId = Guid.NewGuid();
        var mockAddressRepo = new Mock<IRepository<Address>>();
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(mockAddressRepo.Object);

        var request = new SingleAddressRequest(AddressType.Permanent, "123 Main St", null, "Mumbai", "MH", "400001", "India", false);

        await _userService.AddAddressAsync(userId.ToString(), request);

        mockAddressRepo.Verify(r => r.AddAsync(It.Is<Address>(a => a.AddressLine1 == "123 Main St" && a.UserId == userId)), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateAddressAsync_ValidAddress_UpdatesFields()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var address = new Address { Id = addressId, UserId = userId, AddressLine1 = "Old St" };

        var mockAddressRepo = new Mock<IRepository<Address>>();
        mockAddressRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Address, bool>>>())).ReturnsAsync(address);
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(mockAddressRepo.Object);

        var request = new SingleAddressRequest(AddressType.Current, "New St", null, "Delhi", "DL", "110001", "India", false);

        await _userService.UpdateAddressAsync(addressId.ToString(), userId.ToString(), request);

        Assert.That(address.AddressLine1, Is.EqualTo("New St"));
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public async Task DeleteAddressAsync_ValidAddress_DeletesIt()
    {
        var addressId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var address = new Address { Id = addressId, UserId = userId };

        var mockAddressRepo = new Mock<IRepository<Address>>();
        mockAddressRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Address, bool>>>())).ReturnsAsync(address);
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(mockAddressRepo.Object);

        await _userService.DeleteAddressAsync(addressId.ToString(), userId.ToString());

        mockAddressRepo.Verify(r => r.Delete(address), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void DeleteAddressAsync_NotFound_ThrowsKeyNotFound()
    {
        var mockAddressRepo = new Mock<IRepository<Address>>();
        mockAddressRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Address, bool>>>())).ReturnsAsync((Address?)null);
        _mockUnitOfWork.Setup(u => u.Addresses).Returns(mockAddressRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _userService.DeleteAddressAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task DeleteFamilyMemberAsync_ValidMember_DeletesIt()
    {
        var memberId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var member = new CustomerMember { Id = memberId, CustomerId = customerId };

        _mockCustomerRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Customer, bool>>>()))
            .ReturnsAsync(new Customer { Id = customerId, UserId = customerId });
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        mockMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CustomerMember, bool>>>())).ReturnsAsync(member);
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        await _userService.DeleteFamilyMemberAsync(memberId.ToString(), customerId.ToString());

        mockMemberRepo.Verify(r => r.Delete(member), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Test]
    public void DeleteFamilyMemberAsync_NotFound_ThrowsKeyNotFound()
    {
        var mockMemberRepo = new Mock<IRepository<CustomerMember>>();
        mockMemberRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<CustomerMember, bool>>>())).ReturnsAsync((CustomerMember?)null);
        _mockUnitOfWork.Setup(u => u.CustomerMembers).Returns(mockMemberRepo.Object);

        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(() =>
            _userService.DeleteFamilyMemberAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));
    }

    [Test]
    public async Task GetMyKycAsync_RecordExists_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var kycRecord = new KycRecord { Id = Guid.NewGuid(), UserId = userId, KycStatus = KycStatus.Pending, AadhaarNumber = "123456789012", PanNumber = null };
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync(kycRecord);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var result = await _userService.GetMyKycAsync(userId.ToString());

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.KycStatus, Is.EqualTo(KycStatus.Pending.ToString()));
        Assert.That(result.AadhaarUploaded, Is.True);
        Assert.That(result.PanUploaded, Is.False);
    }

    [Test]
    public async Task GetMyKycAsync_NoRecord_ReturnsNull()
    {
        _mockKycRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<KycRecord, bool>>>())).ReturnsAsync((KycRecord?)null);
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var result = await _userService.GetMyKycAsync(Guid.NewGuid().ToString());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetPendingKycAsync_ReturnsPendingRecords()
    {
        var records = new List<KycRecord>
        {
            new KycRecord { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), KycStatus = KycStatus.Pending, AadhaarNumber = "123456789012", PanNumber = "ABCDE1234F" }
        };
        _mockKycRepository.Setup(r => r.GetPagedAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<Expression<Func<KycRecord, bool>>>(), null))
            .ReturnsAsync(((IEnumerable<KycRecord>)records, records.Count));
        _mockUnitOfWork.Setup(u => u.KycRecords).Returns(_mockKycRepository.Object);

        var result = await _userService.GetPendingKycAsync(1, 20);

        Assert.That(result.Data.Count(), Is.EqualTo(1));
        Assert.That(result.TotalRecords, Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllSessionsAsync_ReturnsSessions()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "a@b.com", FirstName = "A", LastName = "B" };
        var sessions = new List<Session>
        {
            new Session { Id = Guid.NewGuid(), UserId = userId, ExpiresAt = DateTime.UtcNow.AddHours(1) }
        };
        var mockSessionRepo = new Mock<IRepository<Session>>();
        mockSessionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(sessions);
        _mockUnitOfWork.Setup(u => u.Sessions).Returns(mockSessionRepo.Object);
        _mockUserRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

        var result = await _userService.GetAllSessionsAsync();

        Assert.That(result.Count(), Is.EqualTo(1));
    }
}
