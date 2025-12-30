using Microsoft.Extensions.Caching.Memory;
using Ticketing.Application.CacheInterfaces;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.CacheServices
{
    public class LocationCacheService : ILocationCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;

        public LocationCacheService(
            IMemoryCache cache,
            IUnitOfWork unitOfWork)
        {
            _cache = cache;
            _unitOfWork = unitOfWork;
        }

        #pragma warning disable CS8602 // Dereference of a possibly null reference.

        public async Task<bool> IsValidStateForCountry(int countryId, int stateId)
        {
            var cacheKey = $"states_by_country_{countryId}";

            var stateIds = await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                    var states = await _unitOfWork.States.GetAll(
                        s => s.CountryId == countryId
                    );

                    var stateIds = states
                        .Select(s => s.Id)
                        .ToHashSet();

                    return stateIds;
                });

            return stateIds.Contains(stateId);
        }
    }
}