using Common.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Common.Caches;

public class UserCache(ChatDbContext dbContext, IDistributedCache cache, ILogger<UserCache> logger) : IUserCache
{
    // Method to get username by userId from cache
    public async Task<string?> GetUserNameAsync(int userId)
    {
        string cacheKey = GetCacheKey(userId);

        // Try to get the username from the cache
        var cachedUserName = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedUserName))
        {
            logger.LogInformation($"Cache hit for userId {userId}");
            return cachedUserName;
        }

        // Fetch from the database if not in cache
        logger.LogInformation($"Cache miss for userId {userId}, fetching from database");
        var user = await dbContext.Users.FindAsync(userId);

        if (user != null)
        {
            // Add to cache for future quick access
            await SetUserNameAsync(userId, user.Username);
            return user.Username;
        }

        return null; // User does not exist
    }
    
    // Method to set username in the cache
    public async Task SetUserNameAsync(int userId, string username)
    {
        string cacheKey = GetCacheKey(userId);

        // Cache the user data with a reasonable expiry time (e.g., 1 hour)
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await cache.SetStringAsync(cacheKey, username, options);
        logger.LogInformation($"Cached username for userId {userId}");
    }

    // Helper method to generate cache keys
    private string GetCacheKey(int userId) => $"user:{userId}:username";

    // Optional method to clear cache for a user
    public async Task RemoveUserFromCacheAsync(int userId)
    {
        string cacheKey = GetCacheKey(userId);
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation($"Removed cache for userId {userId}");
    }

}