namespace Ticketing.Domain.ApiResponse
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        public ApiResponse(bool success, string message, T data, List<string> errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
        }

        public static ApiResponse<T> SuccessResponse(T data, string message = null)
        {
            return new ApiResponse<T>(true, message ?? "Request successful", data);
        }

        public static ApiResponse<T> FailureResponse(List<string> errors, string message = null)
        {
#pragma warning disable CS8604 // Possible null reference argument.
            return new ApiResponse<T>(false, message ?? "Request failed", default, errors);
        }
    }
}



