using AppointmentAPP.Exceptions;
using AppointmentAPP.Helpers;
using System.Text.Json;

namespace AppointmentAPP.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleException(context, ex);
            }
        }

        private static async Task HandleException(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var statusCode = ex is ApiException apiEx ? apiEx.StatusCode : 500;

            var response = statusCode switch
            {
                400 => ResponseModelHelper.BadRequestResult<object>(ex.Message),
                401 => ResponseModelHelper.UnauthorizedResult<object>(ex.Message),
                404 => ResponseModelHelper.NotFoundResult<object>(ex.Message),
                409 => ResponseModelHelper.ConflictResult<object>(ex.Message),
                _ => ResponseModelHelper.BadRequestResult<object>(ex.Message),
            };

            context.Response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }
}