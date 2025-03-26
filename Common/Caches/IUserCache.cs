namespace Common.Caches;

public interface IUserCache
{
    Task<string?> GetUserNameAsync(int userId);
    Task SetUserNameAsync(int userId, string username);
    Task RemoveUserFromCacheAsync(int userId);
}