using AutoMapper;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.Entities;

namespace Ticketing.Api.Configurations
{
    public class MappingProfile : Profile
    {
        #pragma warning disable CS8602 // Dereference of a possibly null reference.
        public MappingProfile()
        {
            CreateMap<UserRequest, User>().ReverseMap();
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName)).ReverseMap();
            CreateMap<User, LoginResponse>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName)).ReverseMap();
            CreateMap<User, UpdateUserRequest>().ReverseMap();
            CreateMap<User, UpdateUserRole>().ReverseMap();
            CreateMap<Role, RoleResponse>().ReverseMap();
            CreateMap<Role, RoleRequest>().ReverseMap();
        }
    }
}