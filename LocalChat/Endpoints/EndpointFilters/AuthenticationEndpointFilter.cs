using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LocalChat.Endpoints.EndpointFilters
{
    // Custom IEndpointFilter for authentication
    public class AuthenticationFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;

            // Retrieve the user ID from claims (authentication check)
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                // Return Unauthorized if no user ID is found in the claims
                return Results.Unauthorized();
            }

            // Continue processing the request if authentication succeeds
            return await next(context);
        }
    }
}