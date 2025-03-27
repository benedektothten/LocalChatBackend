using System.Security.Claims;
using Common.Caches;
using Common.Persistence;
using LocalChat.DTOs;
using LocalChat.Endpoints.EndpointFilters;
using LocalChat.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace LocalChat.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        
        routes.MapPatch("/api/users", async (HttpContext httpContext, UpdateUserDto user, ChatDbContext dbContext, IPasswordHasher passwordHasher) =>
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
    
            if (userIdClaim == null)
            {
                return Results.Unauthorized(); // Return 401 if no user ID claim is found
            }

            var userId = int.Parse(userIdClaim.Value); // Assuming userId is an integer

            // Update the user information
            var existingUser = await dbContext.Users.FindAsync(userId);
            if (existingUser == null)
            {
                return Results.NotFound("User not found.");
            }

            // Apply updates from the DTO (example: username or profile picture)
            existingUser.Username = string.IsNullOrEmpty(user.UserName) ? existingUser.Username : user.UserName;
            existingUser.DisplayName = string.IsNullOrEmpty(user.DisplayName) ? existingUser.Username : user.DisplayName;
            existingUser.PasswordHash = string.IsNullOrEmpty(user.Password) ? existingUser.PasswordHash : passwordHasher.HashPassword(user.Password);
            // TODO: Upload later
            //existingUser.AvatarUrl = user.ProfilePicture ?? existingUser.AvatarUrl; 

            await dbContext.SaveChangesAsync();

            return Results.Accepted($"/api/users/{userId}", user);
        })
        .AddEndpointFilter<AuthenticationFilter>()
        .RequireAuthorization();

        routes.MapGet("/api/user/{id}", async (int id, IUserCache userCache, ChatDbContext dbContext, HttpContext httpContext) =>
            {
                // By this point, the AuthenticationFilter has ensured the user is authenticated (userIdClaim exists)
                var authenticatedUserId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // For basic access control, ensure users can only access their own records
                if (authenticatedUserId != id)
                {
                    return Results.Forbid(); // Return Forbidden if trying to access another user's data
                }

                // Fetch user data from cache or database
                var username = await userCache.GetUserNameAsync(id);

                // If username is null, user does not exist in the database or cache
                if (username == null)
                {
                    return Results.NotFound(); // Return 404 if user not found
                }

                var result = new
                {
                    Id = id,
                    Username = username
                };

                return Results.Ok(result); // Return user data
            })
            .AddEndpointFilter<AuthenticationFilter>()
            .RequireAuthorization();

        routes.MapGet("/api/users", async (ChatDbContext dbContext) =>
            {
                var users = await dbContext.Users
                    .Where(u => u.GoogleId != null)
                    .Select(u => new
                    {
                        u.UserId,
                        u.DisplayName,
                        u.AvatarUrl
                    })
                    .ToListAsync();

                return Results.Ok(users);
            })
            .AddEndpointFilter<AuthenticationFilter>()
            .RequireAuthorization();
    }
}