using Microsoft.Extensions.DependencyInjection;

namespace Common.Caches.Extensions;

public static class ServicesExtensions
{
    public static void AddCaches(this IServiceCollection services)
    {
        services.AddScoped<IUserCache, UserCache>();
    } 
}