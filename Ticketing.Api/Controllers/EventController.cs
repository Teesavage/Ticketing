using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.Interfaces;

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

        [HttpPut("updateEvent/{eventId}")]
        public async Task<IActionResult> UpdateEvent(long eventId, [FromBody] EventRequest request)
        {
            var response = await _eventService.UpdateEvent(eventId, request);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("deleteEvent/{eventId}")]
        public async Task<IActionResult> DeleteEvent(long eventId)
        {
            var response = await _eventService.DeleteEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPatch("deactivateEvent/{eventId}")]
        public async Task<IActionResult> DeactivateEvent(long eventId)
        {
            var response = await _eventService.DeactivateEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPatch("reactivateEvent/{eventId}")]
        public async Task<IActionResult> ReactivateEvent(long eventId)
        {
            var response = await _eventService.ReactivateEvent(eventId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }
               
    }
}