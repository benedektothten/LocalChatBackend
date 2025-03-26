using LocalChat.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LocalChat.Services.Extensions;

public static class ServicesExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
    }
}