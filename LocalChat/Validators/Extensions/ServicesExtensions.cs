using LocalChat.Validators.Interfaces;

namespace LocalChat.Validators.Extensions;

public static class ServicesExtensions
{
    public static void AddValidators(this IServiceCollection services)
    {
        services.AddScoped<ISenderValidator, SenderValidator>();
    }
}