using Ticketing.Application.DTOs.Responses;

namespace Ticketing.Application.CacheInterfaces
{
    public interface ILocationCacheService
    {
        Task<bool> IsValidStateForCountry(int countryId, int stateId);
        Task<IEnumerable<StateResponse>> GetStatesByCountryCached(int countryId);
    }
}