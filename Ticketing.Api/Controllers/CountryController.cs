using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.Interfaces;

namespace Ticketing.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly ICountryService _countryService;

        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateCountry(CountryRequest country)
        {
            var response = await _countryService.CreateCountry(country);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("createBulk")]
        public async Task<IActionResult> CreateCountries(IEnumerable<CountryRequest> countries)
        {
            var response = await _countryService.CreateCountries(countries);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllCountries()
        {
            var response = await _countryService.GetAllCountries();
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getById")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _countryService.GetCountryById(id);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateCountry(int id, CountryRequest country)
        {
            var response = await _countryService.UpdateCountry(id, country);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            var response = await _countryService.DeleteCountry(id);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }


    }

}