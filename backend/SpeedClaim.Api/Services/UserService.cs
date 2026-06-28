using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IEncryptionService _encryptionService;

    public UserService(IUnitOfWork unitOfWork, IStorageService storageService, IEncryptionService encryptionService)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _encryptionService = encryptionService;
    }

    public async Task<UserDto> GetProfileAsync(string userId)
    {
        var uid = Guid.Parse(userId);
        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found");

        var addresses = await _unitOfWork.Addresses.GetPagedAsync(1, 10, a => a.UserId == uid);
        var permanent = addresses.Items.FirstOrDefault(a => a.AddressType == AddressType.Permanent);
        var current = addresses.Items.FirstOrDefault(a => a.AddressType == AddressType.Current);

        var permanentAddressDto = permanent != null ? new AddressDto(permanent.AddressLine1, permanent.AddressLine2, permanent.City, permanent.State, permanent.Pincode, permanent.Country) : new AddressDto("", null, "", "", "", "");
        var currentAddressDto = current != null ? new AddressDto(current.AddressLine1, current.AddressLine2, current.City, current.State, current.Pincode, current.Country) : new AddressDto("", null, "", "", "", "");

        string maritalStatus = "Single";
        Guid? customerId = null;
        if (user.Role == UserRole.Customer)
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uid);
            if (customer != null)
            {
                customerId = customer.Id;
                maritalStatus = customer.MaritalStatus.ToString();
            }
        }

        return new UserDto(
            user.Id,
            user.Email,
            user.Salutation.ToString(),
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}",
            user.Phone,
            user.Role.ToString(),
            maritalStatus,
            customerId,
            user.IsEmailVerified,
            user.IsActive,
            user.CreatedAt,
            permanentAddressDto,
            currentAddressDto
        );
    }

    public async Task UpdateProfileAsync(string userId, UserDto request)
    {
        var uid = Guid.Parse(userId);
        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        
        if (Enum.TryParse<Salutation>(request.Salutation, out var salutation))
        {
            user.Salutation = salutation;
        }

        if (user.Role == UserRole.Customer)
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uid);
            if (customer != null && Enum.TryParse<MaritalStatus>(request.MaritalStatus, out var maritalStatus))
            {
                customer.MaritalStatus = maritalStatus;
            }
        }

        await _unitOfWork.CompleteAsync();
    }

    private async Task<Guid> ResolveCustomerRecordIdAsync(Guid userId)
    {
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) throw new NotFoundException("Customer not found.");
        return customer.Id;
    }

    public async Task<FamilyMemberDto> AddFamilyMemberAsync(string customerId, AddFamilyMemberRequest request)
    {
        var uid = Guid.Parse(customerId);
        var customerRecordId = await ResolveCustomerRecordIdAsync(uid);
        var member = new CustomerMember
        {
            CustomerId = customerRecordId,
            Salutation = request.Salutation,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Relationship = request.Relationship,
            IsDependent = request.IsDependent
        };

        await _unitOfWork.CustomerMembers.AddAsync(member);
        await _unitOfWork.CompleteAsync();

        return new FamilyMemberDto(member.Id, member.Salutation.ToString(), member.FirstName, member.LastName, member.FullName, member.DateOfBirth, member.Gender.ToString(), member.Relationship.ToString(), member.IsDependent);
    }

    public async Task UpdateFamilyMemberAsync(string memberId, string customerId, UpdateFamilyMemberRequest request)
    {
        var mId = Guid.Parse(memberId);
        var cId = await ResolveCustomerRecordIdAsync(Guid.Parse(customerId));
        var member = await _unitOfWork.CustomerMembers.FirstOrDefaultAsync(m => m.Id == mId && m.CustomerId == cId);
        if (member == null) throw new NotFoundException("Family member not found");

        member.Salutation = request.Salutation;
        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.DateOfBirth = request.DateOfBirth;
        member.Gender = request.Gender;
        member.Relationship = request.Relationship;
        member.IsDependent = request.IsDependent;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<FamilyMemberDto>> GetFamilyMembersAsync(string customerId)
    {
        var uid = await ResolveCustomerRecordIdAsync(Guid.Parse(customerId));
        var members = await _unitOfWork.CustomerMembers.GetPagedAsync(1, 100, m => m.CustomerId == uid);
        return members.Items.Select(m => new FamilyMemberDto(m.Id, m.Salutation.ToString(), m.FirstName, m.LastName, m.FullName, m.DateOfBirth, m.Gender.ToString(), m.Relationship.ToString(), m.IsDependent));
    }

    private async Task<KycRecord> GetOrCreateKycRecordAsync(Guid uid)
    {
        var record = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        if (record != null) return record;

        record = new KycRecord { UserId = uid, KycStatus = KycStatus.Pending };
        await _unitOfWork.KycRecords.AddAsync(record);
        return record;
    }

    public async Task UploadAadhaarAsync(string customerId, AadhaarUploadRequest request)
    {
        var uid = Guid.Parse(customerId);

        var existingRecords = await _unitOfWork.KycRecords.FindAsync(k => k.AadhaarNumber != null && k.UserId != uid);
        foreach (var existing in existingRecords)
        {
            if (_encryptionService.Decrypt(existing.AadhaarNumber!) == request.AadhaarNumber)
                throw new ConflictException("Aadhaar number is already registered to another user");
        }

        var record = await GetOrCreateKycRecordAsync(uid);

        record.AadhaarNumber = _encryptionService.Encrypt(request.AadhaarNumber);
        record.KycStatus = KycStatus.Pending;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        var frontKey = $"kyc/{uid}/aadhaar/front";
        using var frontStream = request.FrontDocument.OpenReadStream();
        await _storageService.UploadFileAsync(frontStream, request.FrontDocument.FileName, frontKey);
        record.AadhaarDocumentKeyFront = frontKey;

        if (request.BackDocument != null)
        {
            var backKey = $"kyc/{uid}/aadhaar/back";
            using var backStream = request.BackDocument.OpenReadStream();
            await _storageService.UploadFileAsync(backStream, request.BackDocument.FileName, backKey);
            record.AadhaarDocumentKeyBack = backKey;
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task UploadPanAsync(string customerId, PanUploadRequest request)
    {
        var uid = Guid.Parse(customerId);

        var existingRecords = await _unitOfWork.KycRecords.FindAsync(k => k.PanNumber != null && k.UserId != uid);
        foreach (var existing in existingRecords)
        {
            if (_encryptionService.Decrypt(existing.PanNumber!) == request.PanNumber)
                throw new ConflictException("PAN number is already registered to another user");
        }

        var record = await GetOrCreateKycRecordAsync(uid);

        record.PanNumber = _encryptionService.Encrypt(request.PanNumber);
        record.KycStatus = KycStatus.Pending;
        record.UpdatedAt = DateTimeOffset.UtcNow;

        var frontKey = $"kyc/{uid}/pan/front";
        using var frontStream = request.FrontDocument.OpenReadStream();
        await _storageService.UploadFileAsync(frontStream, request.FrontDocument.FileName, frontKey);
        record.PanDocumentKeyFront = frontKey;

        if (request.BackDocument != null)
        {
            var backKey = $"kyc/{uid}/pan/back";
            using var backStream = request.BackDocument.OpenReadStream();
            await _storageService.UploadFileAsync(backStream, request.BackDocument.FileName, backKey);
            record.PanDocumentKeyBack = backKey;
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task<Guid> AddAddressAsync(string userId, SingleAddressRequest request)
    {
        var uid = Guid.Parse(userId);
        var address = new Address
        {
            UserId = uid,
            AddressType = request.AddressType,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            Pincode = request.PostalCode,
            Country = request.Country,
            IsSameAsPermanent = request.IsSameAsPermanent
        };
        await _unitOfWork.Addresses.AddAsync(address);
        await _unitOfWork.CompleteAsync();
        return address.Id;
    }

    public async Task UpdateAddressAsync(string addressId, string userId, SingleAddressRequest request)
    {
        var aId = Guid.Parse(addressId);
        var uId = Guid.Parse(userId);
        var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == aId && a.UserId == uId);
        if (address == null) throw new NotFoundException("Address not found");

        address.AddressType = request.AddressType;
        address.AddressLine1 = request.AddressLine1;
        address.AddressLine2 = request.AddressLine2;
        address.City = request.City;
        address.State = request.State;
        address.Pincode = request.PostalCode;
        address.Country = request.Country;
        address.IsSameAsPermanent = request.IsSameAsPermanent;
        address.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteAddressAsync(string addressId, string userId)
    {
        var aId = Guid.Parse(addressId);
        var uId = Guid.Parse(userId);
        var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == aId && a.UserId == uId);
        if (address == null) throw new NotFoundException("Address not found.");
        _unitOfWork.Addresses.Delete(address);
        await _unitOfWork.CompleteAsync();
    }

    public async Task DeleteFamilyMemberAsync(string memberId, string customerId)
    {
        var mId = Guid.Parse(memberId);
        var cId = await ResolveCustomerRecordIdAsync(Guid.Parse(customerId));
        var member = await _unitOfWork.CustomerMembers.FirstOrDefaultAsync(m => m.Id == mId && m.CustomerId == cId);
        if (member == null) throw new NotFoundException("Family member not found.");
        _unitOfWork.CustomerMembers.Delete(member);
        await _unitOfWork.CompleteAsync();
    }

    public async Task<PagedResponse<UserDto>> GetAllUsersAsync(int page, int pageSize)
    {
        var (pagedUsers, total) = await _unitOfWork.Users.GetPagedAsync(page, pageSize);
        var dtos = new List<UserDto>();
        foreach (var user in pagedUsers)
        {
            var addresses = await _unitOfWork.Addresses.GetPagedAsync(1, 10, a => a.UserId == user.Id);
            var permanent = addresses.Items.FirstOrDefault(a => a.AddressType == AddressType.Permanent);
            var current = addresses.Items.FirstOrDefault(a => a.AddressType == AddressType.Current);

            var permanentAddressDto = permanent != null ? new AddressDto(permanent.AddressLine1, permanent.AddressLine2, permanent.City, permanent.State, permanent.Pincode, permanent.Country) : new AddressDto("", null, "", "", "", "");
            var currentAddressDto = current != null ? new AddressDto(current.AddressLine1, current.AddressLine2, current.City, current.State, current.Pincode, current.Country) : new AddressDto("", null, "", "", "", "");

            var maritalStatus = "Single";
            Guid? customerId = null;
            if (user.Role == UserRole.Customer)
            {
                var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (customer != null)
                {
                    customerId = customer.Id;
                    maritalStatus = customer.MaritalStatus.ToString();
                }
            }

            dtos.Add(new UserDto(
                user.Id,
                user.Email,
                user.Salutation.ToString(),
                user.FirstName,
                user.LastName,
                $"{user.FirstName} {user.LastName}",
                user.Phone,
                user.Role.ToString(),
                maritalStatus,
                customerId,
                user.IsEmailVerified,
                user.IsActive,
                user.CreatedAt,
                permanentAddressDto,
                currentAddressDto
            ));
        }
        return new PagedResponse<UserDto>(dtos, page, pageSize, total);
    }

    public async Task<PagedResponse<KycRecordDto>> GetPendingKycAsync(int page, int pageSize)
    {
        var (items, total) = await _unitOfWork.KycRecords.GetPagedAsync(
            page, pageSize,
            k => k.KycStatus == KycStatus.Pending && k.AadhaarNumber != null && k.PanNumber != null);
        return new PagedResponse<KycRecordDto>(items.Select(MapToKycDto), page, pageSize, total);
    }

    public async Task UpdateUserRoleAsync(string targetUserId, string role)
    {
        if (!Enum.TryParse<UserRole>(role, out var parsedRole))
            throw new ValidationException("Invalid role");

        var uid = Guid.Parse(targetUserId);
        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found");

        user.Role = parsedRole;
        await _unitOfWork.CompleteAsync();
    }

    private KycRecordDto MapToKycDto(KycRecord k)
    {
        var aadhaarMasked = k.AadhaarNumber != null
            ? _encryptionService.Mask(_encryptionService.Decrypt(k.AadhaarNumber))
            : null;
        var panMasked = k.PanNumber != null
            ? _encryptionService.Mask(_encryptionService.Decrypt(k.PanNumber))
            : null;
        return new KycRecordDto(
            k.Id, k.UserId, k.KycStatus.ToString(),
            k.AadhaarNumber != null, aadhaarMasked,
            k.PanNumber != null, panMasked,
            k.RejectionReason, k.CreatedAt);
    }

    public async Task<KycRecordDto?> GetMyKycAsync(string customerId)
    {
        var uid = Guid.Parse(customerId);
        var kycRecord = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        return kycRecord == null ? null : MapToKycDto(kycRecord);
    }

    public async Task ApproveRejectKycAsync(string customerId, bool isApproved, string reason, string reviewerId)
    {
        var uid = Guid.Parse(customerId);
        var kycRecord = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        if (kycRecord == null) throw new NotFoundException("KYC Record not found");
        if (kycRecord.KycStatus != KycStatus.Pending)
            throw new ConflictException($"KYC has already been {kycRecord.KycStatus}.");

        if (isApproved && (kycRecord.AadhaarNumber == null || kycRecord.PanNumber == null))
            throw new ValidationException("Both Aadhaar and PAN documents must be uploaded before KYC can be approved.");

        kycRecord.KycStatus = isApproved ? KycStatus.Approved : KycStatus.Rejected;
        kycRecord.ReviewedAt = DateTimeOffset.UtcNow;
        kycRecord.ReviewedById = Guid.Parse(reviewerId);
        kycRecord.RejectionReason = reason;

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(reviewerId), EntityType = "KycRecord", EntityId = kycRecord.Id,
            Action = isApproved ? "KycApproved" : "KycRejected", NewValue = JsonSerializer.Serialize(reason), CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();
    }

    public async Task ActivateDeactivateUserAsync(string targetUserId, bool isActive, string adminId)
    {
        var uid = Guid.Parse(targetUserId);
        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found");

        user.IsActive = isActive;
        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<SessionDto>> GetAllSessionsAsync()
    {
        var sessions = await _unitOfWork.Sessions.GetAllAsync();
        var result = new List<SessionDto>();

        foreach (var s in sessions)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(s.UserId);
            result.Add(new SessionDto(
                s.Id,
                s.UserId,
                user?.Email ?? "unknown",
                s.IpAddress,
                s.UserAgent,
                s.ExpiresAt,
                s.IsRevoked,
                s.CreatedAt
            ));
        }

        return result;
    }

    public async Task<IEnumerable<SurveyorDto>> GetSurveyorsAsync()
    {
        var surveyors = await _unitOfWork.Surveyors.FindAsync(s => s.IsActive);
        var result = new List<SurveyorDto>();

        foreach (var s in surveyors)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(s.UserId);
            if (user == null) continue;

            result.Add(new SurveyorDto(
                s.Id,
                user.FirstName,
                user.LastName,
                user.FullName
            ));
        }

        return result;
    }

    public async Task<SurveyorProfileDto> GetSurveyorProfileAsync(string userId)
    {
        var uid = Guid.Parse(userId);
        var surveyor = await _unitOfWork.Surveyors.FirstOrDefaultAsync(s => s.UserId == uid);
        if (surveyor == null) throw new NotFoundException("Surveyor profile not found.");

        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found.");

        return new SurveyorProfileDto(
            surveyor.Id,
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            surveyor.LicenseNumber,
            surveyor.LicenseExpiry,
            surveyor.Specialization.ToString(),
            surveyor.SurveyorType.ToString(),
            surveyor.IsActive
        );
    }
}
