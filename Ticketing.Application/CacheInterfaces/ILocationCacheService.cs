namespace Ticketing.Application.CacheInterfaces
{
    public interface ILocationCacheService
    {
        Task<bool> IsValidStateForCountry(int countryId, int stateId);
    }
}