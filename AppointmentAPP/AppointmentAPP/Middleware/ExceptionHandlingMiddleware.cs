using AppointmentAPP.Exceptions;
using System.Text.Json;

namespace AppointmentAPP.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var statusCode = ex is ApiException apiEx ? apiEx.StatusCode : 500;
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = ex.Message
                });

                await context.Response.WriteAsync(result);
            }
        }
    }
}
