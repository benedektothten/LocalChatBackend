using LocalChat.Common.Persistence;
using LocalChat.Persistence;
using LocalChat.Validators.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace LocalChat.Validators;

public class SenderValidator(ChatDbContext dbContext, IDistributedCache cache, ILogger<SenderValidator> logger)
    : ISenderValidator
{
    public async Task<bool> IsSenderPartOfChatroomAsync(int chatRoomId, int senderId)
    {
        var cacheKey = $"ChatRoom:{chatRoomId}:Sender:{senderId}";

        // Check if the value exists in Redis cache
        var cachedValue = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedValue))
        {
            logger.LogInformation($"Cache hit for {cacheKey}");
            return bool.Parse(cachedValue);
        }

        logger.LogInformation($"Cache miss for {cacheKey}, querying the database...");

        // Query the database
        var isMember = await dbContext.ChatRooms
            .AnyAsync(cu => cu.ChatRoomId == chatRoomId && cu.UserChatRooms.Any(r => r.UserId == senderId));

        // Add to Redis cache with expiration time (e.g., 5 minutes)
        await cache.SetStringAsync(cacheKey, isMember.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return isMember;
    }
}