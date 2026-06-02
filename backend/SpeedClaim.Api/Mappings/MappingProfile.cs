using AutoMapper;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth Mappings
        CreateMap<User, UserDto>()
            .ForCtorParam("Role", opt => opt.MapFrom(src => 
                (src.UserRoles.FirstOrDefault() != null && src.UserRoles.FirstOrDefault()!.Role != null) ? src.UserRoles.FirstOrDefault()!.Role!.Code : "CUSTOMER"));

        // Catalog Mappings
        CreateMap<InsuranceProduct, ProductDto>();

        // Policy Mappings
        CreateMap<Policy, PolicyDto>();
        CreateMap<PolicyHealthDetail, PolicyHealthDetailDto>();
        CreateMap<PolicyVehicleDetail, PolicyVehicleDetailDto>();
        CreateMap<PolicyLifeDetail, PolicyLifeDetailDto>();
    }
}
