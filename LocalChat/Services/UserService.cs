using System.Security.Claims;
using System.Text;
using Common.Settings;
using LocalChat.Common.Persistence;
using LocalChat.DTOs;
using LocalChat.Persistence;
using LocalChat.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LocalChat.Services;

public class UserService(ChatDbContext dbContext, IOptions<JwtSecrets> options) : IUserService
{
    public async Task<(UserEntity userEntity, bool isNewUser)> GetOrCreateUserFromGoogleAsync(GoogleUserDto googleUserDto)
    {
        // Try to find the user based on their Google ID
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.GoogleId == googleUserDto.Id);

        if (user != null)
        {
            // User exists, return the existing user
            return (user, false);
        }

        // If user doesn't exist, create a new user
        user = new UserEntity
        {
            GoogleId = googleUserDto.Id,
            Email = googleUserDto.Email,
            Username = googleUserDto.Name,
            AvatarUrl = googleUserDto.Picture,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = "empty"
        };

        // Add the new user to the database
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return (user, true);

    }

    public string GenerateJwtToken(UserEntity user)
    {
        // Define token handler and key
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var settings = options.Value;
        var key = Encoding.UTF8.GetBytes(settings.JwtTokenSecret); // Replace with a secure key

        // Define token claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
        };

        // Create token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1), // Set token expiry
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = "your-issuer", // Replace with your configured issuer
            Audience = "your-audience" // Replace with your configured audience
        };

        // Create token
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Return serialized token
        return tokenHandler.WriteToken(token);

    }
}