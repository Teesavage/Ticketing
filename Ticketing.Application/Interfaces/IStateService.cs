using Ticketing.Application.DTOs.Requests;
using Ticketing.Application.DTOs.Responses;
using Ticketing.Domain.ApiResponse;

namespace Ticketing.Application.Interfaces
{
    public interface IStateService
    {
        Task<ApiResponse<StateResponse>> CreateState(StateRequest state);
        Task<ApiResponse<IEnumerable<StateResponse>>> CreateStates(IEnumerable<StateRequest> states);
        Task<ApiResponse<StateResponse>> UpdateState(int id, StateRequest state);
        Task<ApiResponse<bool>> DeleteState(int id);
        Task<ApiResponse<int>> DeleteStates(IEnumerable<int> ids);
        Task<ApiResponse<IEnumerable<StateResponse>>> GetStatesByCountry(int countryId);
    }
}