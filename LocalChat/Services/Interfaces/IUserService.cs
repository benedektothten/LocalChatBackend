using LocalChat.DTOs;
using LocalChat.Persistence;

namespace LocalChat.Services.Interfaces;

public interface IUserService
{
    Task<(UserEntity userEntity, bool isNewUser)> GetOrCreateUserFromGoogleAsync(GoogleUserDto googleUserDto);
    
    string GenerateJwtToken(UserEntity userEntity);
}