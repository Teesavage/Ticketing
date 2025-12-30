using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.Interfaces;

namespace Ticketing.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StateController : ControllerBase
    {
        private readonly IStateService _stateService;

        public StateController(IStateService stateService)
        {
            _stateService = stateService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateState(StateRequest state)
        {
            var response = await _stateService.CreateState(state);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("createBulk")]
        public async Task<IActionResult> CreateStates(IEnumerable<StateRequest> states)
        {
            var response = await _stateService.CreateStates(states);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("getStatesByCountry")]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            var response = await _stateService.GetStatesByCountry(countryId);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateState(int id, StateRequest state)
        {
            var response = await _stateService.UpdateState(id, state);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteState(int id)
        {
            var response = await _stateService.DeleteState(id);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("deleteBulk")]
        public async Task<IActionResult> DeleteStates(IEnumerable<int> ids)
        {
            var response = await _stateService.DeleteStates(ids);
            if (!response.Success)
                return BadRequest(response);
            return Ok(response);
        }
    }
}