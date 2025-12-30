using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Ticketing.Application.CacheInterfaces;
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
        private readonly IMemoryCache _cache;
        private readonly ILocationCacheService _locationCacheService;

        public StateService(IUnitOfWork unitOfWork, IMapper mapper, IMemoryCache cache, ILocationCacheService locationCacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
            _locationCacheService = locationCacheService;
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

            //reset cache
            _cache.Remove($"states_by_country_ids_{state.CountryId}");
            _cache.Remove($"states_by_country_list_{state.CountryId}");


            
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

            // Track affected countries
            var affectedCountryIds = new HashSet<int>();

            // Get unique country IDs and validate they exist
            var countryIds = states.Select(s => s.CountryId).Distinct().ToList();
            var existingCountries = await _unitOfWork.Countries
                .GetAll(c => countryIds.Contains(c.Id));

            var validCountryIds = existingCountries.Select(c => c.Id).ToHashSet();

            // Get existing states from DB
            var existingStates = await _unitOfWork.States
                .GetAll(s => countryIds.Contains(s.CountryId));

            var existingStateKeys = existingStates
                .Select(s => $"{s.CountryId}_{s.Name}")
                .ToHashSet();

            var processedStateKeys = new HashSet<string>();

            foreach (var stateRequest in states)
            {
                if (!validCountryIds.Contains(stateRequest.CountryId))
                {
                    errors.Add($"Country ID {stateRequest.CountryId} not found for state '{stateRequest.Name}'.");
                    continue;
                }

                var stateKey = $"{stateRequest.CountryId}_{stateRequest.Name}";

                if (existingStateKeys.Contains(stateKey))
                {
                    errors.Add($"State '{stateRequest.Name}' already exists in country ID {stateRequest.CountryId}.");
                    continue;
                }

                if (processedStateKeys.Contains(stateKey))
                {
                    errors.Add($"Duplicate state '{stateRequest.Name}' for country ID {stateRequest.CountryId} in the request.");
                    continue;
                }

                processedStateKeys.Add(stateKey);

                statesToInsert.Add(_mapper.Map<State>(stateRequest));
                affectedCountryIds.Add(stateRequest.CountryId);
            }

            if (!statesToInsert.Any())
            {
                return ApiResponse<IEnumerable<StateResponse>>.FailureResponse(errors);
            }

            await _unitOfWork.States.InsertRange(statesToInsert);
            await _unitOfWork.Save();

            // Invalidate cache AFTER successful save
            foreach (var countryId in affectedCountryIds)
            {
                _cache.Remove($"states_by_country_ids_{countryId}");
                _cache.Remove($"states_by_country_list_{countryId}");

            }

            successfulResponses = _mapper.Map<List<StateResponse>>(statesToInsert);

            if (errors.Any())
            {
                return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(
                    successfulResponses,
                    $"{successfulResponses.Count} states created successfully. {errors.Count} items skipped."
                );
            }

            return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(successfulResponses);
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

            _cache.Remove($"states_by_country_ids_{state.CountryId}");
            _cache.Remove($"states_by_country_list_{state.CountryId}");

            
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
            
            //reset cache
            _cache.Remove($"states_by_country_ids_{existingState.CountryId}");
            _cache.Remove($"states_by_country_list_{existingState.CountryId}");

            
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

            if (!existingStates.Any())
            {
                return ApiResponse<int>.FailureResponse(["No states found to delete."]);
            }

            // Capture affected country IDs BEFORE deleting
            var affectedCountryIds = existingStates
                .Select(s => s.CountryId)
                .Distinct()
                .ToList();

            var existingIds = existingStates.Select(s => s.Id).ToList();
            var notFoundIds = distinctIds.Except(existingIds).ToList();

            _unitOfWork.States.DeleteRange(existingStates);
            await _unitOfWork.Save();

            // Invalidate cache AFTER successful save
            foreach (var countryId in affectedCountryIds)
            {
                _cache.Remove($"states_by_country_ids_{countryId}");
                _cache.Remove($"states_by_country_list_{countryId}");

            }

            var deletedCount = existingStates.Count;
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

            var states = await _locationCacheService.GetStatesByCountryCached(countryId);
            
            return ApiResponse<IEnumerable<StateResponse>>.SuccessResponse(states);
        }
    }
}