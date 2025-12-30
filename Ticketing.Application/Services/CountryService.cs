using AutoMapper;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.ApiResponse;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.IRespository;

namespace Ticketing.Application.Services
{
    public class CountryService : ICountryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CountryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // Get all countries
        public async Task<ApiResponse<IEnumerable<CountryResponse>>> GetAllCountries()
        {
            var countries = await _unitOfWork.Countries.GetAll();
            var response = _mapper.Map<IEnumerable<CountryResponse>>(countries);
            return ApiResponse<IEnumerable<CountryResponse>>.SuccessResponse(response);
        }

        // Get country by Id
        public async Task<ApiResponse<CountryResponse>> GetCountryById(int id)
        {
            var country = await _unitOfWork.Countries.Get(c => c.Id == id);
            if (country == null)
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Country not found." });

            var response = _mapper.Map<CountryResponse>(country);
            return ApiResponse<CountryResponse>.SuccessResponse(response);
        }

        // Create single country
        public async Task<ApiResponse<CountryResponse>> CreateCountry(CountryRequest country)
        {
            if (country == null)
            {
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Invalid Country Data." });
            }

            // Check if country already exists
            var existingCountry = await _unitOfWork.Countries.Get(c => c.Name == country.Name || c.ISO == country.ISO);

            if (existingCountry != null)
            {
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Country or ISO already exists." });
            }

            var countryEntity = _mapper.Map<Country>(country);
            await _unitOfWork.Countries.Insert(countryEntity);
            await _unitOfWork.Save();
            var response = _mapper.Map<CountryResponse>(countryEntity);
            return ApiResponse<CountryResponse>.SuccessResponse(response);
        }
            
        // Create multiple countries (bulk)
        public async Task<ApiResponse<IEnumerable<CountryResponse>>> CreateCountries(IEnumerable<CountryRequest> countries)
        {
            if (countries == null || !countries.Any())
            {
                return ApiResponse<IEnumerable<CountryResponse>>.FailureResponse(new List<string> { "Invalid Countries Data." });
            }

            var errors = new List<string>();
            var countriesToInsert = new List<Country>();
            var successfulResponses = new List<CountryResponse>();
            
            // Get all country names and ISOs from the request
            var countryNames = countries.Select(c => c.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
            var countryISOs = countries.Select(c => c.ISO).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();

            // Check for existing countries in database (single query for efficiency)
            #pragma warning disable CS8604 // Possible null reference argument.

            var existingCountries = await _unitOfWork.Countries
                .GetAll(c => countryNames.Contains(c.Name) || countryISOs.Contains(c.ISO));

            var existingNames = existingCountries.Select(c => c.Name).ToHashSet();
            var existingISOs = existingCountries.Select(c => c.ISO).ToHashSet();
            
            // Track duplicates within the current batch
            var processedNames = new HashSet<string>();
            var processedISOs = new HashSet<string>();
            
            foreach (var countryRequest in countries)
            {
                // Check if country already exists in database
                if (existingNames.Contains(countryRequest.Name) || existingISOs.Contains(countryRequest.ISO))
                {
                    errors.Add($"Country '{countryRequest.Name}' or ISO '{countryRequest.ISO}' already exists.");
                    continue;
                }
                
                // Check for duplicates within the current request batch
                if (processedNames.Contains(countryRequest.Name) || processedISOs.Contains(countryRequest.ISO))
                {
                    errors.Add($"Duplicate country '{countryRequest.Name}' or ISO '{countryRequest.ISO}' in the request.");
                    continue;
                }
                
                // Mark as processed
                processedNames.Add(countryRequest.Name);
                processedISOs.Add(countryRequest.ISO);
                
                // Map to entity
                var countryEntity = _mapper.Map<Country>(countryRequest);
                countriesToInsert.Add(countryEntity);
            }
            
            // If there are any valid countries to insert
            if (countriesToInsert.Any())
            {
                await _unitOfWork.Countries.InsertRange(countriesToInsert);
                await _unitOfWork.Save();
                
                // Map to responses
                successfulResponses = _mapper.Map<List<CountryResponse>>(countriesToInsert);
                
                // Return success with warnings if some items were skipped
                if (errors.Any())
                {
                    return ApiResponse<IEnumerable<CountryResponse>>.SuccessResponse(
                        successfulResponses,
                        $"{successfulResponses.Count} countries created successfully. {errors.Count} items skipped due to duplicates."
                    );
                }
                
                return ApiResponse<IEnumerable<CountryResponse>>.SuccessResponse(successfulResponses);
            }
            
            // All countries were duplicates
            return ApiResponse<IEnumerable<CountryResponse>>.FailureResponse(errors);
        }

        // Update
        public async Task<ApiResponse<CountryResponse>> UpdateCountry(int id, CountryRequest country)
        {
            if (country == null)
            {
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Invalid Country Data." });
            }

            // Check if country exists
            var existingCountry = await _unitOfWork.Countries.Get(c => c.Id == id);
            
            if (existingCountry == null)
            {
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Country not found." });
            }

            // Check if new name or ISO conflicts with another country
            var duplicateCountry = await _unitOfWork.Countries.Get(c => 
                c.Id != id && (c.Name == country.Name || c.ISO == country.ISO));

            if (duplicateCountry != null)
            {
                return ApiResponse<CountryResponse>.FailureResponse(new List<string> { "Country or ISO already exists." });
            }

            // Map updated values to existing entity
            _mapper.Map(country, existingCountry);
            
            _unitOfWork.Countries.Update(existingCountry);
            await _unitOfWork.Save();
            
            var response = _mapper.Map<CountryResponse>(existingCountry);
            return ApiResponse<CountryResponse>.SuccessResponse(response);
        }

        // Delete single country
        public async Task<ApiResponse<bool>> DeleteCountry(int id)
        {
            var existingCountry = await _unitOfWork.Countries.Get(c => c.Id == id);
            
            if (existingCountry == null)
            {
                return ApiResponse<bool>.FailureResponse(new List<string> { "Country not found." });
            }

            await _unitOfWork.Countries.Delete(existingCountry.Id);
            await _unitOfWork.Save();
            
            return ApiResponse<bool>.SuccessResponse(true, "Country deleted successfully.");
        }

    }

}