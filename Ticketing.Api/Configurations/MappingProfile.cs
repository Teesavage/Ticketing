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
            CreateMap<Event, EventRequest>()
                .ForMember(dest => dest.TicketTypes, opt => opt.MapFrom(src => src.TicketTypes))
                .ReverseMap();
            CreateMap<Event, EventResponse>()
                .ForMember(dest => dest.CreatorName, opt => opt.MapFrom(src => src.Creator.FirstName + " " + src.Creator.LastName))
                .ForMember(dest => dest.EventTypeName, opt => opt.MapFrom(src => src.EventType.ToString()))
                .ReverseMap();
            CreateMap<TicketTypeRequest, TicketType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore());
        }
    }
}