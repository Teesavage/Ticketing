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
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country.Name))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State.Name))
                .ReverseMap();
            CreateMap<TicketTypeRequest, TicketType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ReverseMap();
            CreateMap<TicketTypeResponse, TicketType>().ReverseMap();
            CreateMap<TicketRequest, Ticket>().ReverseMap();
            CreateMap<TicketRequest, TicketResponse>().ReverseMap();
            CreateMap<Ticket, TicketResponse>()
                .ForMember(dest => dest.UserFirstName, opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.UserLastName, opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.TicketType, opt => opt.MapFrom(src => src.TicketType.Type))
                .ForMember(dest => dest.TicketPrice, opt => opt.MapFrom(src => src.TicketType.Price))
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.TicketType.Event.Id))
                .ForMember(dest => dest.EventTitle, opt => opt.MapFrom(src => src.TicketType.Event.EventTitle))
                .ForMember(dest => dest.EventDateTime, opt => opt.MapFrom(src => src.TicketType.Event.EventDateTime))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.TicketType.Event.Country.Name))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.TicketType.Event.State.Name))
                .ReverseMap();

            CreateMap<Country, CountryResponse>().ReverseMap();
            CreateMap<Country, CountryRequest>().ReverseMap();
            CreateMap<State, StateResponse>()
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country.Name))
            .ReverseMap();
            CreateMap<State, StateRequest>().ReverseMap();
        }
    }
}