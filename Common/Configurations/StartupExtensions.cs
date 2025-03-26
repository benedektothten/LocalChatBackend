using Azure.Identity;
using Common.Settings;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Configurations;

public static class StartupExtensions
{
    /// <summary>
    /// Adds Azure Key Vault configuration to the application.
    /// </summary>
    public static void AddAzureKeyVaultConfiguration(this IConfiguration configuration,
        IConfigurationBuilder configBuilder)
    {
        var keyVaultAlreadyAdded = (configuration as IConfigurationRoot)?.Providers
            .Any(provider => provider.GetType().Name.Contains("AzureKeyVault", StringComparison.OrdinalIgnoreCase)) ?? false;

        if (keyVaultAlreadyAdded) return;
        var keyVaultName = configuration["KeyVaultName"];

        if (string.IsNullOrEmpty(keyVaultName))
        {
            throw new InvalidOperationException("Key Vault name is not configured.");
        }
        
        configBuilder.AddAzureKeyVault(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
    }

    /// <summary>
    /// Adds Azure Service Bus configuration and support to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration"></param>
    /// <param name="sectionName"></param>
    public static void AddAzureServiceBus(this IServiceCollection services, IConfiguration configuration, string sectionName = nameof(AzureServiceBusSettings))
    {
        // Ensure Azure Key Vault is configured
        configuration.AddAzureKeyVaultConfiguration(configuration as IConfigurationBuilder);

        // If the Azure Service Bus settings are stored as a section in Key Vault
        var connectionStringFromKeyVault = configuration["AzureServiceBusConnectionString"]; // Secret from Azure Key Vault

        if (string.IsNullOrEmpty(connectionStringFromKeyVault))
        {
            throw new InvalidOperationException("Azure Service Bus connection string is not configured in Key Vault.");
        }
        
        var queueNameFromKeyVault = configuration["AzureServiceBusQueueName"]; // Secret from Azure Key Vault

        if (string.IsNullOrEmpty(queueNameFromKeyVault))
        {
            throw new InvalidOperationException("Azure Service Bus queue name is not configured in Key Vault.");
        }

        // Manually map the settings if other properties like `QueueName` are also configured in Key Vault or appsettings.json
        var serviceBusSettings = new AzureServiceBusSettings
        {
            ConnectionString = connectionStringFromKeyVault, 
            QueueName = queueNameFromKeyVault // Also allow QueueName to remain in appsettings.json
        };

        services.Configure<AzureServiceBusSettings>(options =>
        {
            options.ConnectionString = serviceBusSettings.ConnectionString;
            options.QueueName = serviceBusSettings.QueueName;
        });

        //var settings = configuration.GetSection(nameof(AzureServiceBusSettings)).Get<AzureServiceBusSettings>();
        
        if (serviceBusSettings?.ConnectionString is null || serviceBusSettings?.QueueName is null)
            throw new InvalidOperationException($"Azure Service Bus settings are not configured.");

        if (string.IsNullOrEmpty(serviceBusSettings?.ConnectionString))
        {
            throw new InvalidOperationException("Azure Service Bus connection string is not configured.");
        }
        
        services.AddAzureClients(s =>
        {
            s.AddServiceBusClient(serviceBusSettings.ConnectionString);
        });
    }

    /// <summary>
    /// Adds Redis caching support to the service collection.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration"></param>
    public static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        // Ensure Azure Key Vault is configured
        configuration.AddAzureKeyVaultConfiguration(configuration as IConfigurationBuilder);

        services.AddStackExchangeRedisCache(options =>
        {
            var redisConnectionString = configuration["RedisConnectionString"];
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new InvalidOperationException("Redis connection string is not configured.");
            }
            options.Configuration = redisConnectionString;
        });
    }

}