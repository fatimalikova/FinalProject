using AppointmentAPP.Models;

namespace AppointmentAPP.Helpers
{
    public class ResponseModelHelper
    {
        public static ResponseModel<T> SuccessResult<T>(T data) => new()
        {
            Success = true,
            Errors = new List<string>(),
            Data = data,
            StatusCode = 200
        };

        public static ResponseModel<T> CreatedResult<T>(T data) => new()
        {
            Success = true,
            Errors = new List<string>(),
            Data = data,
            StatusCode = 201
        };

        public static ResponseModel<T> BadRequestResult<T>(params string[] errors) => new()
        {
            Success = false,
            Errors = new List<string>(errors),
            Data = default,
            StatusCode = 400
        };

        public static ResponseModel<T> UnauthorizedResult<T>(params string[] errors) => new()
        {
            Success = false,
            Errors = errors.Length > 0 ? new List<string>(errors) : new List<string> { "Unauthorized" },
            Data = default,
            StatusCode = 401
        };

        public static ResponseModel<T> NotFoundResult<T>(params string[] errors) => new()
        {
            Success = false,
            Errors = errors.Length > 0 ? new List<string>(errors) : new List<string> { "Not found" },
            Data = default,
            StatusCode = 404
        };

        public static ResponseModel<T> ConflictResult<T>(params string[] errors) => new()
        {
            Success = false,
            Errors = errors.Length > 0 ? new List<string>(errors) : new List<string> { "Conflict" },
            Data = default,
            StatusCode = 409
        };

        public static ResponseModel<T> ErrorResult<T>(params string[] errors) => new()
        {
            Success = false,
            Errors = new List<string>(errors),
            Data = default,
            StatusCode = 500
        };
    }
}
