using AppointmentAPP.Helpers;
using System.Text.Json;

namespace AppointmentAPP.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

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

            var response = ResponseModelHelper
                .ErrorResult<object>(ex.Message);

            context.Response.StatusCode = response.StatusCode;

            var json = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(json);
        }
    }
}
