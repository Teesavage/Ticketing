using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Application.Interfaces;
using Ticketing.Domain;
using Ticketing.Domain.ApiResponse;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public EventService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<ApiResponse<EventResponse>> AddEvent(EventRequest newEvent)
        {
            if (newEvent is null)
                return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event is null" });

            var errors = new List<string>();

            // Validation
            if (string.IsNullOrWhiteSpace(newEvent.EventTitle))
                errors.Add("Event title is required");

            if (string.IsNullOrWhiteSpace(newEvent.EventDescription))
                errors.Add("Event description is required");

            // if (string.IsNullOrWhiteSpace(newEvent.OrganizerEmail))
            //     errors.Add("Organizer email is required");
            // else if (!IsValidEmail(newEvent.OrganizerEmail))
            //     errors.Add("Invalid organizer email format");

            if (string.IsNullOrWhiteSpace(newEvent.Location))
                errors.Add("Location is required");

            if (newEvent.EventDate < DateOnly.FromDateTime(DateTime.UtcNow))
                errors.Add("Event date cannot be in the past");

            if (string.IsNullOrWhiteSpace(newEvent.EventTime))
                errors.Add("Event time is required");

            if (!Enum.IsDefined(typeof(EventType), newEvent.EventType))
            {
                errors.Add("Invalid event type");
            }
            if (newEvent.TicketTypes == null || !newEvent.TicketTypes.Any())
                errors.Add("At least one ticket type is required");
            else
            {
                // Validate each ticket type
                for (int i = 0; i < newEvent.TicketTypes.Count; i++)
                {
                    var ticket = newEvent.TicketTypes[i];
                    
                    if (string.IsNullOrWhiteSpace(ticket.Type))
                        errors.Add($"Ticket type name is required for ticket {i + 1}");
                    
                    if (ticket.Price < 0)
                        errors.Add($"Ticket price cannot be negative for '{ticket.Type}'");
                    
                    if (ticket.QuantityAvailable <= 0)
                        errors.Add($"Ticket quantity must be greater than zero for '{ticket.Type}'");
                }

                // Check for duplicate ticket types
                var duplicateTypes = newEvent.TicketTypes
                    .GroupBy(t => t.Type?.Trim().ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateTypes.Any())
                    errors.Add($"Duplicate ticket types found: {string.Join(", ", duplicateTypes)}");
            }

            if (newEvent.CreatedBy == Guid.Empty)
                errors.Add("CreatedBy user ID is required");

            if (errors.Any())
                return ApiResponse<EventResponse>.FailureResponse(errors);

            try
            {
                // Check if user exists 
                var user = await _unitOfWork.Users.Get(u => u.Id == newEvent.CreatedBy);
                if (user == null)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "User not found" });

                // Map request to entity
                var eventEntity = _mapper.Map<Event>(newEvent);
                
                // Set additional properties
                eventEntity.OrganizerEmail = user.Email;
                eventEntity.OrganizerPhoneNo = user.PhoneNumber ?? null;
                eventEntity.CreatedAt = DateTime.UtcNow;
                eventEntity.IsActive = true;

                // Map and set up ticket types
                eventEntity.TicketTypes = newEvent.TicketTypes.Select(ticketRequest => new TicketType
                {
                    Type = ticketRequest.Type?.Trim(),
                    Price = ticketRequest.Price,
                    QuantityAvailable = ticketRequest.QuantityAvailable,
                    EventId = eventEntity.Id
                }).ToList();

                // Add event to database (this will also add ticket types if cascade is configured)
                await _unitOfWork.Events.Insert(eventEntity);
                await _unitOfWork.Save();

                // Map entity to response
                var eventResponse = _mapper.Map<EventResponse>(eventEntity);

                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event created successfully");
            }
            catch (Exception ex)
            {                
                return ApiResponse<EventResponse>.FailureResponse(
                    new List<string> {ex.Message, "An error occurred while creating the event. Please try again." }
                );
            }
        }

        // Helper method for email validation
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // GET EVENT BY ID
        public async Task<ApiResponse<EventResponse>> GetEventById(long eventId)
        {
            try
            {
                var eventEntity = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes", "Creator"]
                );

                if (eventEntity == null)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event not found" });

                var eventResponse = _mapper.Map<EventResponse>(eventEntity);
                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventResponse>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while retrieving the event." }
                );
            }
        }

        // GET ALL EVENTS
        public async Task<ApiResponse<IEnumerable<EventResponse>>> GetAllEvents(
            bool includeInactive = false,
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                Expression<Func<Event, bool>>? filter = includeInactive ? null : e => e.IsActive;

                var events = await _unitOfWork.Events.GetAll(
                    includes: ["TicketTypes", "Creator"],
                    orderBy: q => q.OrderByDescending(e => e.CreatedAt)
                );

                // Apply pagination
                var paginatedEvents = events
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var eventResponses = _mapper.Map<IEnumerable<EventResponse>>(paginatedEvents);

                return ApiResponse<IEnumerable<EventResponse>>.SuccessResponse(
                    eventResponses,
                    $"Retrieved {eventResponses.Count()} events successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<EventResponse>>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while retrieving events." }
                );
            }
        }

        // Updates event details only (no ticketTypes)
        public async Task<ApiResponse<EventResponse>> UpdateEvent(long eventId, EventUpdateRequest updatedEvent)
        {
            if (updatedEvent is null)
                return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event data is null" });

            var errors = new List<string>();

            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event not found" });

                // Validation - only validate fields that are being updated (not null)
                if (updatedEvent.EventTitle != null && string.IsNullOrWhiteSpace(updatedEvent.EventTitle))
                    errors.Add("Event title cannot be empty");

                if (updatedEvent.EventDescription != null && string.IsNullOrWhiteSpace(updatedEvent.EventDescription))
                    errors.Add("Event description cannot be empty");

                if (updatedEvent.Location != null && string.IsNullOrWhiteSpace(updatedEvent.Location))
                    errors.Add("Location cannot be empty");

                if (updatedEvent.EventDate.HasValue && updatedEvent.EventDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                    errors.Add("Event date cannot be in the past");

                if (updatedEvent.EventTime != null && string.IsNullOrWhiteSpace(updatedEvent.EventTime))
                    errors.Add("Event time cannot be empty");

                if (updatedEvent.EventType.HasValue && !Enum.IsDefined(typeof(EventType), updatedEvent.EventType.Value))
                    errors.Add("Invalid event type");

                if (errors.Any())
                    return ApiResponse<EventResponse>.FailureResponse(errors);

                // Update only non-null properties
                if (!string.IsNullOrWhiteSpace(updatedEvent.EventTitle))
                    existingEvent.EventTitle = updatedEvent.EventTitle;

                if (!string.IsNullOrWhiteSpace(updatedEvent.EventDescription))
                    existingEvent.EventDescription = updatedEvent.EventDescription;

                if (!string.IsNullOrWhiteSpace(updatedEvent.Location))
                    existingEvent.Location = updatedEvent.Location;

                if (updatedEvent.EventDate.HasValue)
                    existingEvent.EventDate = updatedEvent.EventDate.Value;

                if (!string.IsNullOrWhiteSpace(updatedEvent.EventTime))
                    existingEvent.EventTime = updatedEvent.EventTime;

                if (updatedEvent.EventType.HasValue)
                    existingEvent.EventType = updatedEvent.EventType.Value;

                existingEvent.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Events.Update(existingEvent);
                await _unitOfWork.Save();

                var eventResponse = _mapper.Map<EventResponse>(existingEvent);
                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventResponse>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while updating the event." }
                );
            }
        }

        //  Add new ticket types
        public async Task<ApiResponse<List<TicketTypeResponse>>> AddTicketTypes(long eventId, List<TicketTypeRequest> ticketTypes)
        {
            if (ticketTypes == null || !ticketTypes.Any())
                return ApiResponse<List<TicketTypeResponse>>.FailureResponse(new List<string> { "At least one ticket type is required" });

            var errors = new List<string>();

            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<List<TicketTypeResponse>>.FailureResponse(new List<string> { "Event not found" });

                // Validation
                for (int i = 0; i < ticketTypes.Count; i++)
                {
                    var ticket = ticketTypes[i];
                    
                    if (string.IsNullOrWhiteSpace(ticket.Type))
                        errors.Add($"Ticket type name is required for ticket {i + 1}");
                    
                    if (ticket.Price < 0)
                        errors.Add($"Ticket price cannot be negative for '{ticket.Type}'");
                    
                    if (ticket.QuantityAvailable <= 0)
                        errors.Add($"Ticket quantity must be greater than zero for '{ticket.Type}'");
                }

                // Check for duplicates in the request
                var duplicateTypes = ticketTypes
                    .GroupBy(t => t.Type?.Trim().ToLower())
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateTypes.Any())
                    errors.Add($"Duplicate ticket types in request: {string.Join(", ", duplicateTypes)}");

                // Check for duplicates with existing tickets
                if (existingEvent.TicketTypes != null && existingEvent.TicketTypes.Any())
                {
                    var existingTypes = existingEvent.TicketTypes.Select(t => t.Type?.Trim().ToLower()).ToList();
                    var conflictingTypes = ticketTypes
                        .Where(t => existingTypes.Contains(t.Type?.Trim().ToLower()))
                        .Select(t => t.Type)
                        .ToList();

                    if (conflictingTypes.Any())
                        errors.Add($"Ticket types already exist: {string.Join(", ", conflictingTypes)}");
                }

                if (errors.Any())
                    return ApiResponse<List<TicketTypeResponse>>.FailureResponse(errors);

                // Add new ticket types
                var newTickets = ticketTypes.Select(ticketRequest => new TicketType
                {
                    Type = ticketRequest.Type?.Trim(),
                    Price = ticketRequest.Price,
                    QuantityAvailable = ticketRequest.QuantityAvailable,
                    EventId = eventId
                }).ToList();

                await _unitOfWork.TicketTypes.InsertRange(newTickets);
                await _unitOfWork.Save();

                var ticketResponses = _mapper.Map<List<TicketTypeResponse>>(newTickets);
                return ApiResponse<List<TicketTypeResponse>>.SuccessResponse(ticketResponses, "Ticket types added successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TicketTypeResponse>>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while adding ticket types." }
                );
            }
        }

        // Update specific event ticket type
        public async Task<ApiResponse<TicketTypeResponse>> UpdateTicketType(long eventId, long ticketTypeId, TicketTypeRequest ticketType)
        {
            if (ticketType is null)
                return ApiResponse<TicketTypeResponse>.FailureResponse(new List<string> { "Ticket type data is null" });

            var errors = new List<string>();

            // Validation
            if (string.IsNullOrWhiteSpace(ticketType.Type))
                errors.Add("Ticket type name is required");

            if (ticketType.Price < 0)
                errors.Add("Ticket price cannot be negative");

            if (ticketType.QuantityAvailable <= 0)
                errors.Add("Ticket quantity must be greater than zero");

            if (errors.Any())
                return ApiResponse<TicketTypeResponse>.FailureResponse(errors);

            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<TicketTypeResponse>.FailureResponse(new List<string> { "Event not found" });

                var existingTicket = existingEvent.TicketTypes?.FirstOrDefault(t => t.Id == ticketTypeId);

                if (existingTicket == null)
                    return ApiResponse<TicketTypeResponse>.FailureResponse(new List<string> { "Ticket type not found" });

                // Check if the new type name conflicts with other existing tickets
                var conflictingTicket = existingEvent.TicketTypes?
                    .FirstOrDefault(t => t.Id != ticketTypeId && 
                                        t.Type?.Trim().ToLower() == ticketType.Type?.Trim().ToLower());

                if (conflictingTicket != null)
                    return ApiResponse<TicketTypeResponse>.FailureResponse(
                        new List<string> { $"Ticket type '{ticketType.Type}' already exists for this event" });

                // Update ticket properties
                existingTicket.Type = ticketType.Type?.Trim();
                existingTicket.Price = ticketType.Price;
                existingTicket.QuantityAvailable = ticketType.QuantityAvailable;

                _unitOfWork.TicketTypes.Update(existingTicket);
                await _unitOfWork.Save();

                var ticketResponse = _mapper.Map<TicketTypeResponse>(existingTicket);
                return ApiResponse<TicketTypeResponse>.SuccessResponse(ticketResponse, "Ticket type updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<TicketTypeResponse>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while updating the ticket type." }
                );
            }
        }

        // Remove ticket type
        public async Task<ApiResponse<bool>> DeleteTicketType(long eventId, long ticketTypeId)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<bool>.FailureResponse(new List<string> { "Event not found" });

                var existingTicket = existingEvent.TicketTypes?.FirstOrDefault(t => t.Id == ticketTypeId);

                if (existingTicket == null)
                    return ApiResponse<bool>.FailureResponse(new List<string> { "Ticket type not found" });

                // Check if this is the last ticket type
                if (existingEvent.TicketTypes?.Count == 1)
                    return ApiResponse<bool>.FailureResponse(
                        new List<string> { "Cannot delete the last ticket type. An event must have at least one ticket type." });

                // Optional: Check if there are any bookings for this ticket type
                // If you have a Bookings table, you might want to prevent deletion if bookings exist
                // var hasBookings = await _unitOfWork.Bookings.Exists(b => b.TicketTypeId == ticketId);
                // if (hasBookings)
                //     return ApiResponse<bool>.FailureResponse(
                //         new List<string> { "Cannot delete ticket type with existing bookings" });

                await _unitOfWork.TicketTypes.Delete(existingTicket.Id);
                await _unitOfWork.Save();

                return ApiResponse<bool>.SuccessResponse(true, "Ticket type deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while deleting the ticket type." }
                );
            }
        }

        // DELETE EVENT (Hard Delete)
        public async Task<ApiResponse<bool>> DeleteEvent(long eventId)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<bool>.FailureResponse(new List<string> { "Event not found" });

                // Delete ticket types first (if not configured for cascade delete)
                if (existingEvent.TicketTypes != null && existingEvent.TicketTypes.Any())
                {
                    _unitOfWork.TicketTypes.DeleteRange(existingEvent.TicketTypes);
                }

                await _unitOfWork.Events.Delete(existingEvent.Id);
                await _unitOfWork.Save();

                return ApiResponse<bool>.SuccessResponse(true, "Event deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while deleting the event." }
                );
            }
        }

        // DEACTIVATE EVENT (Soft Delete)
        public async Task<ApiResponse<EventResponse>> DeactivateEvent(long eventId)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event not found" });

                if (!existingEvent.IsActive)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event is already inactive" });

                existingEvent.IsActive = false;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Events.Update(existingEvent);
                await _unitOfWork.Save();

                var eventResponse = _mapper.Map<EventResponse>(existingEvent);
                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event deactivated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventResponse>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while deactivating the event." }
                );
            }
        }

        // REACTIVATE EVENT
        public async Task<ApiResponse<EventResponse>> ReactivateEvent(long eventId)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event not found" });

                if (existingEvent.IsActive)
                    return ApiResponse<EventResponse>.FailureResponse(new List<string> { "Event is already active" });

                existingEvent.IsActive = true;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Events.Update(existingEvent);
                await _unitOfWork.Save();

                var eventResponse = _mapper.Map<EventResponse>(existingEvent);
                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event reactivated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventResponse>.FailureResponse(
                    new List<string> { ex.Message, "An error occurred while reactivating the event." }
                );
            }
        }


    }
}