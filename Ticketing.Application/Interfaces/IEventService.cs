using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Application.Interfaces
{
    public interface IEventService
    {
        public Task<ApiResponse<EventResponse>> AddEvent(EventRequest request);
        Task<ApiResponse<EventResponse>> GetEventById(long eventId);
        Task<ApiResponse<IEnumerable<EventResponse>>> GetAllEvents(
            bool includeInactive = false,
            int pageNumber = 1,
            int pageSize = 10);
        Task<ApiResponse<EventResponse>> UpdateEvent(long eventId, EventUpdateRequest updatedEvent);
        Task<ApiResponse<List<TicketTypeResponse>>> GetEventTicketTypes(long eventId);
        Task<ApiResponse<List<TicketTypeResponse>>> AddTicketTypes(long eventId, List<TicketTypeRequest> ticketTypes);
        Task<ApiResponse<TicketTypeResponse>> UpdateTicketType(long eventId, long ticketTypeId, TicketTypeRequest ticketType);
        Task<ApiResponse<bool>> DeleteTicketType(long eventId, long ticketTypeId);
        Task<ApiResponse<EventResponse>> DeactivateEvent(long eventId);
        Task<ApiResponse<EventResponse>> ReactivateEvent(long eventId);
        Task<ApiResponse<bool>> DeleteEvent(long eventId);
        Task<ApiResponse<TicketResponse>> AddTicket(TicketRequest ticket);
        Task<ApiResponse<TicketResponse>> GetTicketById(Guid ticketId);
        Task<ApiResponse<PageResponse<TicketResponse>>> GetUserTickets(
            Guid userId,
            PaginationFilter paginationFilter,
            bool includeInactive = false
        );
        Task<ApiResponse<PageResponse<TicketResponse>>> GetAllTickets(
            PaginationFilter paginationFilter,
            bool includeInactive = false
        );
    }
}