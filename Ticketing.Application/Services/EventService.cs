using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Ticketing.Application.CacheInterfaces;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Application.Interfaces;
using Ticketing.Domain;
using Ticketing.Domain.ApiResponse;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;
using Ticketing.Infrastructure.PaginationHelper;

namespace Ticketing.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;
        private readonly ILocationCacheService _locationCacheService;

        public EventService(IMapper mapper, IUnitOfWork unitOfWork, IConfiguration config, ILocationCacheService locationCacheService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _config = config;
            _locationCacheService = locationCacheService;
        }

        #pragma warning disable CS8604 // Possible null reference argument.

        public async Task<ApiResponse<EventResponse>> AddEvent(EventRequest newEvent)
        {
            if (newEvent is null)
                return ApiResponse<EventResponse>.FailureResponse(["Event is null"]);

            var errors = new List<string>();

            // Validation
            if (string.IsNullOrWhiteSpace(newEvent.EventTitle))
                errors.Add("Event title is required");

            if (string.IsNullOrWhiteSpace(newEvent.EventDescription))
                errors.Add("Event description is required");
            if (newEvent.CountryId <= 0)
                errors.Add("Valid CountryId is required");
            if (newEvent.StateId <= 0)
                errors.Add("Valid StateId is required");

            // if (string.IsNullOrWhiteSpace(newEvent.OrganizerEmail))
            //     errors.Add("Organizer email is required");
            // else if (!IsValidEmail(newEvent.OrganizerEmail))
            //     errors.Add("Invalid organizer email format");

            if (string.IsNullOrWhiteSpace(newEvent.Location))
                errors.Add("Location is required");

            if (newEvent.EventDateTime < DateTime.UtcNow)
                errors.Add("Event date cannot be in the past");

            // if (string.IsNullOrWhiteSpace(newEvent.EventTime))
            //     errors.Add("Event time is required");

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
                    return ApiResponse<EventResponse>.FailureResponse(["User not found"]);

                // Cached location validation
                if (!await _locationCacheService.IsValidStateForCountry(newEvent.CountryId, newEvent.StateId))
                {
                    return ApiResponse<EventResponse>.FailureResponse(["Invalid state for the selected country"]);
                }

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
                    [ex.Message, "An error occurred while creating the event. Please try again."]
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
                    includes: ["TicketTypes", "Creator", "Country", "State"]
                );

                if (eventEntity == null)
                    return ApiResponse<EventResponse>.FailureResponse(["Event not found"]);

                var eventResponse = _mapper.Map<EventResponse>(eventEntity);
                return ApiResponse<EventResponse>.SuccessResponse(eventResponse, "Event retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventResponse>.FailureResponse(
                    [ex.Message, "An error occurred while retrieving the event."]
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
                Expression<Func<Event, bool>>? filter = includeInactive ? null : e => e.IsActive == true;

                var events = await _unitOfWork.Events.GetAll(
                    expression: filter,
                    includes: ["TicketTypes", "Creator", "Country", "State"],
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
                    [ex.Message, "An error occurred while retrieving events."]
                );
            }
        }

        // Updates event details only (no ticketTypes)
        #pragma warning disable CS8629 // Nullable value type may be null.

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

                // Validation - only for non-null updates
                if (updatedEvent.EventTitle != null && string.IsNullOrWhiteSpace(updatedEvent.EventTitle))
                    errors.Add("Event title cannot be empty");

                if (updatedEvent.EventDescription != null && string.IsNullOrWhiteSpace(updatedEvent.EventDescription))
                    errors.Add("Event description cannot be empty");

                if (updatedEvent.Location != null && string.IsNullOrWhiteSpace(updatedEvent.Location))
                    errors.Add("Location cannot be empty");

                if (updatedEvent.EventDateTime.HasValue && updatedEvent.EventDateTime.Value < DateTime.UtcNow)
                    errors.Add("Event date cannot be in the past");

                if (updatedEvent.EventType.HasValue && !Enum.IsDefined(typeof(EventType), updatedEvent.EventType.Value))
                    errors.Add("Invalid event type");

                // ✅ Validate Country + State only if being updated
                if (updatedEvent.CountryId.HasValue || updatedEvent.StateId.HasValue)
                {
                    int countryIdToCheck = updatedEvent.CountryId ?? (int)existingEvent.CountryId;
                    int stateIdToCheck = updatedEvent.StateId ?? (int)existingEvent.StateId;

                    bool isValidLocation = await _locationCacheService.IsValidStateForCountry(countryIdToCheck, stateIdToCheck);
                    if (!isValidLocation)
                        errors.Add("Invalid state or state does not belong to the selected country");
                }

                if (errors.Any())
                    return ApiResponse<EventResponse>.FailureResponse(errors);

                // Update only non-null properties
                if (!string.IsNullOrWhiteSpace(updatedEvent.EventTitle))
                    existingEvent.EventTitle = updatedEvent.EventTitle;

                if (!string.IsNullOrWhiteSpace(updatedEvent.EventDescription))
                    existingEvent.EventDescription = updatedEvent.EventDescription;

                if (!string.IsNullOrWhiteSpace(updatedEvent.Location))
                    existingEvent.Location = updatedEvent.Location;

                if (updatedEvent.EventDateTime.HasValue)
                    existingEvent.EventDateTime = updatedEvent.EventDateTime.Value;

                if (updatedEvent.EventType.HasValue)
                    existingEvent.EventType = updatedEvent.EventType.Value;

                // Update Country + State if provided
                if (updatedEvent.CountryId.HasValue)
                    existingEvent.CountryId = updatedEvent.CountryId.Value;

                if (updatedEvent.StateId.HasValue)
                    existingEvent.StateId = updatedEvent.StateId.Value;

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
                return ApiResponse<List<TicketTypeResponse>>.FailureResponse(["At least one ticket type is required"]);

            var errors = new List<string>();

            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<List<TicketTypeResponse>>.FailureResponse(["Event not found"]);

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
                    [ex.Message, "An error occurred while adding ticket types."]
                );
            }
        }

        //Get Event TicketTypes
        public async Task<ApiResponse<List<TicketTypeResponse>>> GetEventTicketTypes(long eventId)
        {
            try
            {
                var existingEvent = await _unitOfWork.Events.Get(
                    e => e.Id == eventId,
                    includes: ["TicketTypes"]
                );

                if (existingEvent == null)
                    return ApiResponse<List<TicketTypeResponse>>.FailureResponse(["Event not found"]);

                var ticketResponses = _mapper.Map<List<TicketTypeResponse>>(existingEvent.TicketTypes);
                return ApiResponse<List<TicketTypeResponse>>.SuccessResponse(ticketResponses, "Ticket types retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<List<TicketTypeResponse>>.FailureResponse(
                    [ex.Message, "An error occurred while retrieving ticket types."]
                );
            }
        }

        // Update specific event ticket type
        public async Task<ApiResponse<TicketTypeResponse>> UpdateTicketType(long eventId, long ticketTypeId, TicketTypeRequest ticketType)
        {
            if (ticketType is null)
                return ApiResponse<TicketTypeResponse>.FailureResponse(["Ticket type data is null"]);

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
                    return ApiResponse<TicketTypeResponse>.FailureResponse(["Event not found"]);

                var existingTicket = existingEvent.TicketTypes?.FirstOrDefault(t => t.Id == ticketTypeId);

                if (existingTicket == null)
                    return ApiResponse<TicketTypeResponse>.FailureResponse(["Ticket type not found"]);

                // Check if the new type name conflicts with other existing tickets
                var conflictingTicket = existingEvent.TicketTypes?
                    .FirstOrDefault(t => t.Id != ticketTypeId && 
                                        t.Type?.Trim().ToLower() == ticketType.Type?.Trim().ToLower());

                if (conflictingTicket != null)
                    return ApiResponse<TicketTypeResponse>.FailureResponse(
                        [$"Ticket type '{ticketType.Type}' already exists for this event"]);

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
                    [ex.Message, "An error occurred while updating the ticket type."]
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
                    return ApiResponse<bool>.FailureResponse(["Event not found"]);

                var existingTicket = existingEvent.TicketTypes?.FirstOrDefault(t => t.Id == ticketTypeId);

                if (existingTicket == null)
                    return ApiResponse<bool>.FailureResponse(["Ticket type not found"]);

                // Check if this is the last ticket type
                if (existingEvent.TicketTypes?.Count == 1)
                    return ApiResponse<bool>.FailureResponse(
                        ["Cannot delete the last ticket type. An event must have at least one ticket type."]);

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
                    [ex.Message, "An error occurred while deleting the ticket type."]
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
                    return ApiResponse<bool>.FailureResponse(["Event not found"]);

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
                    [ex.Message, "An error occurred while deleting the event."]
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
                    return ApiResponse<EventResponse>.FailureResponse(["Event not found"]);

                if (!existingEvent.IsActive)
                    return ApiResponse<EventResponse>.FailureResponse(["Event is already inactive"]);

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
                    [ex.Message, "An error occurred while deactivating the event."]
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
                    return ApiResponse<EventResponse>.FailureResponse(["Event not found"]);

                if (existingEvent.IsActive)
                    return ApiResponse<EventResponse>.FailureResponse(["Event is already active"]);

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
                    [ex.Message, "An error occurred while reactivating the event."]
                );
            }
        }

        public async Task<ApiResponse<TicketResponse>> AddTicket(TicketRequest ticket)
        {
            if (ticket is null)
                return ApiResponse<TicketResponse>.FailureResponse(["Ticket is null"]);

            try
            {
                // Check if TicketType exists 
                var ticketType = await _unitOfWork.TicketTypes.GetWithTracking(tt => tt.Id == ticket.TicketTypeId);
                if (ticketType == null)
                    return ApiResponse<TicketResponse>.FailureResponse(["TicketType not found"]);

                if (ticketType.QuantityAvailable < 1)
                    return ApiResponse<TicketResponse>.FailureResponse(["TicketType Sold Out"]);
                if (ticket.Quantity < 1)
                    return ApiResponse<TicketResponse>.FailureResponse(["Quantity must be at least 1"]);
                if (ticket.Quantity > ticketType.QuantityAvailable)
                    return ApiResponse<TicketResponse>.FailureResponse(
                        [$"Only {ticketType.QuantityAvailable} tickets are available for this TicketType"]
                    );

                // Check if user exists
                var user = await _unitOfWork.Users.Get(u => u.Id == ticket.UserId);
                if (user == null)
                    return ApiResponse<TicketResponse>.FailureResponse(["User not found"]);

                // Create multiple ticket entities based on quantity
                var purchaseTime = DateTime.UtcNow;
                var ticketEntities = new List<Ticket>();
                
                for (int i = 0; i < ticket.Quantity; i++)
                {
                    var ticketEntity = _mapper.Map<Ticket>(ticket);
                    ticketEntity.PurchaseTime = purchaseTime;
                    ticketEntities.Add(ticketEntity);
                }

                // Insert all tickets at once using InsertRange
                await _unitOfWork.Tickets.InsertRange(ticketEntities);

                // Decrease available quantity
                ticketType.QuantityAvailable -= ticket.Quantity;

                await _unitOfWork.Save();

                // Map first ticket to response and include quantity purchased
                var ticketResponse = _mapper.Map<TicketResponse>(ticketEntities.First());
                ticketResponse.Quantity = ticket.Quantity;

                return ApiResponse<TicketResponse>.SuccessResponse(
                    ticketResponse, 
                    $"{ticket.Quantity} ticket(s) added successfully"
                );
            }
            catch (Exception ex)
            {
                return ApiResponse<TicketResponse>.FailureResponse(
                    [ex.Message, "An error occurred while creating the ticket. Please try again."]
                );
            }
        }

        public async Task<ApiResponse<TicketResponse>> GetTicketById(Guid ticketId)
        {
            try
            {
                var ticket = await _unitOfWork.Tickets.Get(
                t => t.TicketId == ticketId,
                includes: ["User", "TicketType", "TicketType.Event"]);

                if (ticket is null)
                    return ApiResponse<TicketResponse>.FailureResponse(["Ticket not found"]);

                 var ticketResponse = _mapper.Map<TicketResponse>(ticket);
                 ticketResponse.Quantity = 1; // Since we're fetching a single ticket

                 return ApiResponse<TicketResponse>.SuccessResponse(ticketResponse, "Ticket retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<TicketResponse>.FailureResponse(
                    [ex.Message, "An error occurred while retrieving the ticket. Please try again."]
                );
            }
        }

        public async Task<ApiResponse<PageResponse<TicketResponse>>> GetUserTickets(
            Guid userId,
            PaginationFilter paginationFilter,
            bool includeInactive = false
        )
        {
            try
            {
                var tickets = await _unitOfWork.Tickets.GetAll(
                    t => t.UserId == userId,
                    includes: ["User", "TicketType", "TicketType.Event"],
                    orderBy: q => q.OrderByDescending(e => e.PurchaseTime)
                );

                // Apply filters in-memory
                if (!includeInactive)
                {
                    tickets = tickets.Where(t => t.IsActive).ToList();
                }

                if (!string.IsNullOrWhiteSpace(paginationFilter.Search))
                {
                    tickets = tickets.Where(t => 
                        t.TicketType?.Event?.EventTitle?.Contains(
                            paginationFilter.Search, 
                            StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (tickets == null || !tickets.Any())
                {
                    var emptyMeta = new Meta(0, paginationFilter.PageNumber, paginationFilter.PageSize);
                    var emptyResponse = new PageResponse<TicketResponse>(
                        // Enumerable.Empty<TicketResponse>(), 
                        [],// alternative to line above
                        emptyMeta
                    );
                    
                    return ApiResponse<PageResponse<TicketResponse>>.SuccessResponse(
                        emptyResponse,
                        "No tickets found"
                    );
                }

                var totalCount = tickets.Count;
                
                // Apply pagination
                var paginatedTickets = tickets
                    .Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                    .Take(paginationFilter.PageSize)
                    .ToList();

                var ticketResponses = _mapper.Map<IEnumerable<TicketResponse>>(paginatedTickets);

                // Set quantity for each ticket
                foreach (var ticket in ticketResponses)
                {
                    ticket.Quantity = 1;
                }

                // Create pagination metadata
                var meta = new Meta(totalCount, paginationFilter.PageNumber, paginationFilter.PageSize);
                
                // Create paginated response
                var pageResponse = new PageResponse<TicketResponse>(ticketResponses, meta);

                return ApiResponse<PageResponse<TicketResponse>>.SuccessResponse(
                    pageResponse,
                    $"Retrieved {paginatedTickets.Count} of {totalCount} tickets"
                );
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging
                return ApiResponse<PageResponse<TicketResponse>>.FailureResponse(
                    ["An error occurred while retrieving tickets. Please try again.", ex.Message]
                );
            }
        }
        public async Task<ApiResponse<PageResponse<TicketResponse>>> GetAllTickets(
            PaginationFilter paginationFilter,
            bool includeInactive = false
        )
        {
            try
            {
                var tickets = await _unitOfWork.Tickets.GetAll(
                    includes: ["User", "TicketType", "TicketType.Event"],
                    orderBy: q => q.OrderByDescending(e => e.PurchaseTime)
                );

                // Apply filters in-memory
                if (!includeInactive)
                {
                    tickets = tickets.Where(t => t.IsActive).ToList();
                }

                if (!string.IsNullOrWhiteSpace(paginationFilter.Search))
                {
                    tickets = tickets.Where(t => 
                        t.TicketType?.Event?.EventTitle?.Contains(
                            paginationFilter.Search, 
                            StringComparison.OrdinalIgnoreCase) == true
                    ).ToList();
                }

                if (tickets == null || !tickets.Any())
                {
                    var emptyMeta = new Meta(0, paginationFilter.PageNumber, paginationFilter.PageSize);
                    var emptyResponse = new PageResponse<TicketResponse>(
                        // Enumerable.Empty<TicketResponse>(), 
                        [],// alternative to line above
                        emptyMeta
                    );
                    
                    return ApiResponse<PageResponse<TicketResponse>>.SuccessResponse(
                        emptyResponse,
                        "No tickets found"
                    );
                }

                var totalCount = tickets.Count;
                
                // Apply pagination
                var paginatedTickets = tickets
                    .Skip((paginationFilter.PageNumber - 1) * paginationFilter.PageSize)
                    .Take(paginationFilter.PageSize)
                    .ToList();

                var ticketResponses = _mapper.Map<IEnumerable<TicketResponse>>(paginatedTickets);

                // Set quantity for each ticket
                foreach (var ticket in ticketResponses)
                {
                    ticket.Quantity = 1;
                }

                // Create pagination metadata
                var meta = new Meta(totalCount, paginationFilter.PageNumber, paginationFilter.PageSize);
                
                // Create paginated response
                var pageResponse = new PageResponse<TicketResponse>(ticketResponses, meta);

                return ApiResponse<PageResponse<TicketResponse>>.SuccessResponse(
                    pageResponse,
                    $"Retrieved {paginatedTickets.Count} of {totalCount} tickets"
                );
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging
                return ApiResponse<PageResponse<TicketResponse>>.FailureResponse(
                    ["An error occurred while retrieving tickets. Please try again.", ex.Message]
                );
            }
        }

        public async Task<bool> IsValidState(int countryId, int stateId)
        {
            return await _locationCacheService.IsValidStateForCountry(countryId, stateId);
        }

    }
}