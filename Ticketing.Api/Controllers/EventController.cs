using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.Interfaces;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService;
        }
        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPost("createEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
        {
            var response = await _eventService.AddEvent(request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getEventById/{eventId}")]
        public async Task<IActionResult> GetEventById(long eventId)
        {
            var response = await _eventService.GetEventById(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getAllEvents")]
        public async Task<IActionResult> GetAllEvents(
            [FromQuery] bool includeInactive = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var response = await _eventService.GetAllEvents(includeInactive, pageNumber, pageSize);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPut("updateEvent/{eventId}")]
        public async Task<IActionResult> UpdateEvent(long eventId, [FromBody] EventUpdateRequest request)
        {
            var response = await _eventService.UpdateEvent(eventId, request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPost("addTicketTypes")]
        public async Task<IActionResult> AddTicketTypes(long eventId, [FromBody] List<TicketTypeRequest> ticketTypes)
        {
            var response = await _eventService.AddTicketTypes(eventId, ticketTypes);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getEventTicketTypes")]
        public async Task<IActionResult> GetEventTicketTypes(long eventId)
        {
            var response = await _eventService.GetEventTicketTypes(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPut("updateTicketTypes")]
        public async Task<IActionResult> UpdateTicketType(long eventId, long ticketTypeId, [FromBody] TicketTypeRequest ticketType)
        {
            var response = await _eventService.UpdateTicketType(eventId, ticketTypeId, ticketType);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpDelete("deleteTicketType")]
        public async Task<IActionResult> DeleteTicketType(long eventId, long ticketTypeId)
        {
            var response = await _eventService.DeleteTicketType(eventId, ticketTypeId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpDelete("deleteEvent/{eventId}")]
        public async Task<IActionResult> DeleteEvent(long eventId)
        {
            var response = await _eventService.DeleteEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPatch("deactivateEvent/{eventId}")]
        public async Task<IActionResult> DeactivateEvent(long eventId)
        {
            var response = await _eventService.DeactivateEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        // [Authorize(Roles = "Manager, Admin, Organizer")]
        [HttpPatch("reactivateEvent/{eventId}")]
        public async Task<IActionResult> ReactivateEvent(long eventId)
        {
            var response = await _eventService.ReactivateEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("addTicket")]
        public async Task<IActionResult> AddTicket([FromBody] TicketRequest ticket)
        {
            var response = await _eventService.AddTicket(ticket);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getTicketById")]
        public async Task<IActionResult> GetTicketById(Guid ticketId)
        {
            var response = await _eventService.GetTicketById(ticketId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getUserTickets")]
        public async Task<IActionResult> GetUserTickets(Guid userId, [FromQuery] PaginationFilter paginationFilter, [FromQuery] bool includeInactive = false)
        {
            var response = await _eventService.GetUserTickets(userId, paginationFilter, includeInactive);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getAllTickets")]
        public async Task<IActionResult> GetAllTickets([FromQuery] PaginationFilter paginationFilter, [FromQuery] bool includeInactive = false)
        {
            var response = await _eventService.GetAllTickets(paginationFilter, includeInactive);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("isValidState")]
        public async Task<IActionResult> IsValidState(int countryId, int stateId)
        {
            var isValid = await _eventService.IsValidState(countryId, stateId);
            return Ok(isValid);
        }

    }
}