using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace LocalChat.Services;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Extract UserID from claims
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}