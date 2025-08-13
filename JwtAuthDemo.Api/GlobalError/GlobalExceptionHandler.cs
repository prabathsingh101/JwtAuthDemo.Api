using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

namespace JwtAuthDemo.Api.GlobalError
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");

            var (status, title) = MapException(exception);

            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = title,
                Detail = _env.IsDevelopment() ? exception.Message : "An error occurred while processing your request.",
                Type = $"https://httpstatuses.com/{(int)status}"
            };

            httpContext.Response.StatusCode = problem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            var json = JsonSerializer.Serialize(problem);
            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }

        private static (HttpStatusCode Status, string Title) MapException(Exception ex) =>
            ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, "Validation error"),
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),                
                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Concurrency conflict"),
                DbUpdateException => (HttpStatusCode.Conflict, "Database update error"),
                NotImplementedException => (HttpStatusCode.NotImplemented, "Not implemented"),

                _ => (HttpStatusCode.InternalServerError, "Server error")

            };
    }
}
