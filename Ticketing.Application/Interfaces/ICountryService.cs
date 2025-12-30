using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Interfaces
{
    public interface ICountryService
    {
        Task<ApiResponse<IEnumerable<CountryResponse>>> GetAllCountries();
        Task<ApiResponse<CountryResponse>> GetCountryById(int id);
        Task<ApiResponse<CountryResponse>> CreateCountry(CountryRequest country);
        Task<ApiResponse<IEnumerable<CountryResponse>>> CreateCountries(IEnumerable<CountryRequest> countries);
        Task<ApiResponse<CountryResponse>> UpdateCountry(int id, CountryRequest country);
        Task<ApiResponse<bool>> DeleteCountry(int id);
    }

}