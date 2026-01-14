using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Ticketing.Application.CacheInterfaces;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.CacheServices
{
    public class LocationCacheService : ILocationCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<LocationCacheService> _logger;

        public LocationCacheService(
            IMemoryCache cache,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<LocationCacheService> logger)
        {
            _cache = cache;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        #pragma warning disable CS8602 // Dereference of a possibly null reference.
        #pragma warning disable CS8603 // Possible null reference return.


        public async Task<bool> IsValidStateForCountry(int countryId, int stateId)
        {
            // var cacheKey = $"states_by_country_{countryId}";
            var cacheKey = $"states_by_country_ids_{countryId}";

            var stateIds = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    _logger.LogInformation($"CACHE MISS: {cacheKey}");

                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    var states = await _unitOfWork.States.GetAll(
                        s => s.CountryId == countryId
                    );

                    var stateIds = states
                        .Select(s => s.Id)
                        .ToHashSet();

                    return stateIds;
                });

            _logger.LogInformation($"CACHE HIT: {cacheKey}");

            return stateIds.Contains(stateId);
        }

        public async Task<IEnumerable<StateResponse>> GetStatesByCountryCached(int countryId)
        {
            var cacheKey = $"states_by_country_list_{countryId}";

            return await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    _logger.LogInformation($"CACHE MISS: {cacheKey}");

                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    var states = await _unitOfWork.States.GetAll(
                        s => s.CountryId == countryId,
                        includes: ["Country"]
                    );
                    _logger.LogInformation($"CACHE HIT: {cacheKey}");

                    return _mapper.Map<IEnumerable<StateResponse>>(states);
                });
        }

    }
}
