using System;
using System.Collections.Generic;
using System.Linq;
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
        if (user.Role == UserRole.Customer)
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == uid);
            if (customer != null)
            {
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

    public async Task<FamilyMemberDto> AddFamilyMemberAsync(string customerId, AddFamilyMemberRequest request)
    {
        var uid = Guid.Parse(customerId);
        var member = new CustomerMember
        {
            CustomerId = uid,
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
        var cId = Guid.Parse(customerId);
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
        var uid = Guid.Parse(customerId);
        var members = await _unitOfWork.CustomerMembers.GetPagedAsync(1, 100, m => m.CustomerId == uid);
        return members.Items.Select(m => new FamilyMemberDto(m.Id, m.Salutation.ToString(), m.FirstName, m.LastName, m.FullName, m.DateOfBirth, m.Gender.ToString(), m.Relationship.ToString(), m.IsDependent));
    }

    public async Task UploadKycDocumentsAsync(string customerId, KycUploadRequest request)
    {
        var uid = Guid.Parse(customerId);
        var kycRecord = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        if (kycRecord == null)
        {
            kycRecord = new KycRecord
            {
                UserId = uid,
                KycStatus = KycStatus.Pending,
                IdType = request.IdType,
                IdNumber = _encryptionService.Encrypt(request.IdNumber)
            };
            await _unitOfWork.KycRecords.AddAsync(kycRecord);
        }
        else
        {
            kycRecord.IdType = request.IdType;
            kycRecord.IdNumber = _encryptionService.Encrypt(request.IdNumber);
            kycRecord.KycStatus = KycStatus.Pending;
            kycRecord.UpdatedAt = DateTimeOffset.UtcNow;
        }

        using var frontStream = request.FrontDocument.OpenReadStream();
        await _storageService.UploadFileAsync(frontStream, request.FrontDocument.FileName, $"kyc/{uid}/front");

        if (request.BackDocument != null)
        {
            using var backStream = request.BackDocument.OpenReadStream();
            await _storageService.UploadFileAsync(backStream, request.BackDocument.FileName, $"kyc/{uid}/back");
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task AddAddressAsync(string userId, SingleAddressRequest request)
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
            Pincode = request.Pincode,
            Country = request.Country,
            IsSameAsPermanent = request.IsSameAsPermanent
        };
        await _unitOfWork.Addresses.AddAsync(address);
        await _unitOfWork.CompleteAsync();
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
        address.Pincode = request.Pincode;
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
        var cId = Guid.Parse(customerId);
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
            if (user.Role == UserRole.Customer)
            {
                var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (customer != null)
                    maritalStatus = customer.MaritalStatus.ToString();
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
                permanentAddressDto,
                currentAddressDto
            ));
        }
        return new PagedResponse<UserDto>(dtos, page, pageSize, total);
    }

    public async Task<PagedResponse<KycRecordDto>> GetPendingKycAsync(int page, int pageSize)
    {
        var (items, total) = await _unitOfWork.KycRecords.GetPagedAsync(page, pageSize, k => k.KycStatus == KycStatus.Pending);
        return new PagedResponse<KycRecordDto>(
            items.Select(k => new KycRecordDto(k.Id, k.UserId, k.KycStatus.ToString(), k.IdType.ToString(), _encryptionService.Decrypt(k.IdNumber), k.CreatedAt)),
            page, pageSize, total);
    }

    public async Task UpdateUserRoleAsync(string targetUserId, string role)
    {
        var uid = Guid.Parse(targetUserId);
        var user = await _unitOfWork.Users.GetByIdAsync(uid);
        if (user == null) throw new NotFoundException("User not found");

        if (Enum.TryParse<UserRole>(role, out var parsedRole))
        {
            user.Role = parsedRole;
            await _unitOfWork.CompleteAsync();
        }
        else
        {
            throw new ValidationException("Invalid role");
        }
    }

    public async Task<KycRecordDto?> GetMyKycAsync(string customerId)
    {
        var uid = Guid.Parse(customerId);
        var kycRecord = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        if (kycRecord == null) return null;
        
        var decrypted = _encryptionService.Decrypt(kycRecord.IdNumber);
        return new KycRecordDto(kycRecord.Id, kycRecord.UserId, kycRecord.KycStatus.ToString(), kycRecord.IdType.ToString(), _encryptionService.Mask(decrypted), kycRecord.CreatedAt);
    }

    public async Task ApproveRejectKycAsync(string customerId, bool isApproved, string reason, string reviewerId)
    {
        var uid = Guid.Parse(customerId);
        var kycRecord = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == uid);
        if (kycRecord == null) throw new NotFoundException("KYC Record not found");

        kycRecord.KycStatus = isApproved ? KycStatus.Approved : KycStatus.Rejected;
        kycRecord.ReviewedAt = DateTimeOffset.UtcNow;
        kycRecord.ReviewedById = Guid.Parse(reviewerId);
        kycRecord.RejectionReason = reason;
        
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
}
