using AutoMapper;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.ApiResponse;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.Services
{
    public class StateService : IStateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StateService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Create single state
        public async Task<ApiResponse<StateResponse>> CreateState(StateRequest state)
        {
            if (state == null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["Invalid State Data."]);
            }

            // Check if country exists
            var country = await _unitOfWork.Countries.Get(s => s.Id == state.CountryId);
            
            if (country == null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["Country not found."]);
            }

            // Check if state already exists in this country
            var existingState = await _unitOfWork.States.Get(s => 
                s.CountryId == state.CountryId && s.Name == state.Name);

            if (existingState != null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["State already exists in this country."]);
            }

            var stateEntity = _mapper.Map<State>(state);
            await _unitOfWork.States.Insert(stateEntity);
            await _unitOfWork.Save();
            
            var response = _mapper.Map<StateResponse>(stateEntity);
            return ApiResponse<StateResponse>.SuccessResponse(response);
        }

        // Create multiple states (bulk)
        public async Task<ApiResponse<IEnumerable<StateResponse>>> CreateStates(IEnumerable<StateRequest> states)
        {
            if (states == null || !states.Any())
            {
                return ApiResponse<IEnumerable<StateResponse>>.FailureResponse(["Invalid States Data."]);
            }

            var errors = new List<string>();
            var statesToInsert = new List<State>();
            var successfulResponses = new List<StateResponse>();
            
            // Get unique country IDs and validate they exist
            var countryIds = states.Select(s => s.CountryId).Distinct().ToList();
            var existingCountries = await _unitOfWork.Countries
                .GetAll(c => countryIds.Contains(c.Id));
            var validCountryIds = existingCountries.Select(c => c.Id).ToHashSet();
            
            // Get all state names grouped by country from request
            var statesByCountry = states.GroupBy(s => s.CountryId)
                .ToDictionary(g => g.Key, g => g.Select(s => s.Name).ToList());
            
            // Check for existing states in database
            var existingStates = await _unitOfWork.States
                .GetAll(s => countryIds.Contains(s.CountryId));
            
            var existingStateKeys = existingStates
                .Select(s => $"{s.CountryId}_{s.Name}")
                .ToHashSet();
            
            // Track duplicates within the current batch
            var processedStateKeys = new HashSet<string>();
            
            foreach (var stateRequest in states)
            {
                // Validate country exists
                if (!validCountryIds.Contains(stateRequest.CountryId))
                {
                    errors.Add($"Country ID {stateRequest.CountryId} not found for state '{stateRequest.Name}'.");
                    continue;
                }
                
                var stateKey = $"{stateRequest.CountryId}_{stateRequest.Name}";
                
                // Check if state already exists in database
                if (existingStateKeys.Contains(stateKey))
                {
                    errors.Add($"State '{stateRequest.Name}' already exists in country ID {stateRequest.CountryId}.");
                    continue;
                }
                
                // Check for duplicates within the current request batch
                if (processedStateKeys.Contains(stateKey))
                {
                    errors.Add($"Duplicate state '{stateRequest.Name}' for country ID {stateRequest.CountryId} in the request.");
                    continue;
                }
                
                // Mark as processed
                processedStateKeys.Add(stateKey);
                
                // Map to entity
                var stateEntity = _mapper.Map<State>(stateRequest);
                statesToInsert.Add(stateEntity);
            }
            
            // If there are any valid states to insert
            if (statesToInsert.Any())
            {
                await _unitOfWork.States.InsertRange(statesToInsert);
                await _unitOfWork.Save();
                
                // Map to responses
                successfulResponses = _mapper.Map<List<StateResponse>>(statesToInsert);
                
                // Return success with warnings if some items were skipped
                if (errors.Any())
                {
                    return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(
                        successfulResponses,
                        $"{successfulResponses.Count} states created successfully. {errors.Count} items skipped. {errors}"
                    );
                }
                
                return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(successfulResponses);
            }
            
            // All states were invalid or duplicates
            return ApiResponse<IEnumerable<StateResponse>>.FailureResponse(errors);
        }

        // Update single state
        public async Task<ApiResponse<StateResponse>> UpdateState(int id, StateRequest state)
        {
            if (state == null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["Invalid State Data."]);
            }

            // Check if state exists
            var existingState = await _unitOfWork.States.Get(s => s.Id == id);
            
            if (existingState == null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["State not found."]);
            }

            // Check if country exists
            var country = await _unitOfWork.Countries.Get(s => s.Id == state.CountryId);
            
            if (country == null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["Country not found."]);
            }

            // Check if new name conflicts with another state in the same country
            var duplicateState = await _unitOfWork.States.Get(s => 
                s.Id != id && s.CountryId == state.CountryId && s.Name == state.Name);

            if (duplicateState != null)
            {
                return ApiResponse<StateResponse>.FailureResponse(["State already exists in this country."]);
            }

            // Map updated values to existing entity
            _mapper.Map(state, existingState);
            
            _unitOfWork.States.Update(existingState);
            await _unitOfWork.Save();
            
            var response = _mapper.Map<StateResponse>(existingState);
            return ApiResponse<StateResponse>.SuccessResponse(response);
        }

        // Delete single state
        public async Task<ApiResponse<bool>> DeleteState(int id)
        {
            var existingState = await _unitOfWork.States.Get(s => s.Id == id);
            
            if (existingState == null)
            {
                return ApiResponse<bool>.FailureResponse(["State not found."]);
            }

            await _unitOfWork.States.Delete(existingState.Id);
            await _unitOfWork.Save();
            
            return ApiResponse<bool>.SuccessResponse(true, "State deleted successfully.");
        }

        // Bulk delete states
        public async Task<ApiResponse<int>> DeleteStates(IEnumerable<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return ApiResponse<int>.FailureResponse(["No state IDs provided."]);
            }

            var distinctIds = ids.Distinct().ToList();
            
            // Get all states that exist
            var existingStates = await _unitOfWork.States
                .GetAll(s => distinctIds.Contains(s.Id));
            
            var existingIds = existingStates.Select(s => s.Id).ToList();
            var notFoundIds = distinctIds.Except(existingIds).ToList();
            
            if (!existingStates.Any())
            {
                return ApiResponse<int>.FailureResponse(["No states found to delete."]);
            }

            _unitOfWork.States.DeleteRange(existingStates);
            await _unitOfWork.Save();
            
            var deletedCount = existingStates.Count();
            var message = $"{deletedCount} states deleted successfully.";
            
            if (notFoundIds.Any())
            {
                message += $" {notFoundIds.Count} IDs were not found.";
            }
            
            return ApiResponse<int>.SuccessResponse(deletedCount, message);
        }

        // Get states by country
        public async Task<ApiResponse<IEnumerable<StateResponse>>> GetStatesByCountry(int countryId)
        {
            // Check if country exists
            var country = await _unitOfWork.Countries.Get(c => c.Id == countryId);
            
            if (country == null)
            {
                return ApiResponse<IEnumerable<StateResponse>>.FailureResponse(["Country not found."]);
            }

            var states = await _unitOfWork.States.GetAll(s => s.CountryId == countryId, includes: ["Country"]);
            var response = _mapper.Map<IEnumerable<StateResponse>>(states);
            
            return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(response);
        }
    }
}