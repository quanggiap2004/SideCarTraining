using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SideCar.Business.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SideCar.Business.Helpers.Exceptions
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> _logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var statusCode = exception switch
            {
                ValidationException         => StatusCodes.Status400BadRequest,
                ArgumentException           => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                KeyNotFoundException        => StatusCodes.Status404NotFound,
                BusinessException           => StatusCodes.Status400BadRequest,
                _                           => StatusCodes.Status500InternalServerError
            };

            BaseResponse<object> response;

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
                response = new BaseResponse<object>("Internal Server Error", new { traceId });
            }
            else
            {
                response = new BaseResponse<object>(exception.Message, null);
            }

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            return true;
        }
    }
}
