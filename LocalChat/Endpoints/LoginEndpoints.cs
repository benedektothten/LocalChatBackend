using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Common.Settings;
using Google.Apis.Auth;
using LocalChat.DTOs;
using LocalChat.Services;
using LocalChat.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LocalChat.Endpoints;

public static class LoginEndpoints
{
    /// <summary>
    /// Maps the login-related endpoints for the application.
    /// </summary>
    /// <param name="routes">The <see cref="IEndpointRouteBuilder"/> to define the routes.</param>
    public static void MapLoginEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/login", async context =>
            {
                // Forces the user into Google login flow
                var properties = new AuthenticationProperties { RedirectUri = "/" }; // Redirect to home after login
                await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
            }
        );
        
        routes.Map("/signin-google", async context =>
        {
            var result = await context.AuthenticateAsync();
            if (result.Succeeded)
            {
                var claims = result.Principal?.Claims.Select(c => new { c.Type, c.Value });
                await context.Response.WriteAsync($"User authenticated: {System.Text.Json.JsonSerializer.Serialize(claims)}");
            }
            else
            {
                await context.Response.WriteAsync("Authentication failed");
            }
        });
        
        
        routes.MapPost("/verify-token", async (HttpContext context, IUserService userService) =>
        {
            var requestBody = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            var token = requestBody?["token"];

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 400; // Bad Request
                await context.Response.WriteAsync("Token is required");
                return null;
            }

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(token);
        
                // Example: Extract user info from payload
                var googleUserDto = new GoogleUserDto(payload.Subject, payload.Email, payload.Name, payload.Picture);
                var (user, isNewUser) = await userService.GetOrCreateUserFromGoogleAsync(googleUserDto);
                var jwtToken = userService.GenerateJwtToken(user);

                // Perform any additional checks or actions (e.g., save user to DB)
                context.Response.StatusCode = 200; // Success
                return new LoginResponse(user.UserId, user.AvatarUrl, isNewUser, jwtToken);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync($"Invalid token: {ex.Message}");
                return null;
            }
        });
        
        routes.MapGet("/validate-token", async (HttpContext context, IOptions<JwtSecrets> jwtSecrets) =>
        {
            // Get the Authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 400; // Bad Request
                await context.Response.WriteAsync("Invalid or missing Authorization header.");
                return;
            }

            var token = authHeader["Bearer ".Length..].Trim(); // Extract the JWT token from the header

            // Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecrets.Value.JwtTokenSecret); // Retrieve the secret key
            
            try
            {
                // Set the token validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "your-issuer", // Replace with your issuer
                    ValidateAudience = true,
                    ValidAudience = "your-audience", // Replace with your audience
                    ValidateLifetime = true, // Ensure the token is not expired
                    ClockSkew = TimeSpan.Zero // Reduce clock skew time
                };

                // Validate the token and extract the principal
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // Ensure the token is a valid JWT token
                if (validatedToken is not JwtSecurityToken jwtToken || 
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid token.");
                    return;
                }

                // Token is valid, return the decoded claims
                var claims = principal.Claims.Select(c => new { c.Type, c.Value });
                context.Response.StatusCode = 200; // OK
                await context.Response.WriteAsJsonAsync(new
                {
                    IsValid = true,
                    Claims = claims
                });
            }
            catch (Exception ex)
            {
                // Token validation failed
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsJsonAsync(new
                {
                    IsValid = false,
                    Message = $"Token validation failed: {ex.Message}"
                });
            }
        });

    }
}