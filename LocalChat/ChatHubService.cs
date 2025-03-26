using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace LocalChat;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> Connections = new(); // Key: ConnectionId, Value: UserId
    
    // Send a message to all clients in the chat room group
    public async Task SendMessage(string chatRoomId, Guid messageUniqueId, int userId, string userName, string message, bool isGif)
    {
        // Send the message to the group associated with the chat room ID
        await Clients.Group(chatRoomId).SendAsync("ReceiveMessage", messageUniqueId, userId, userName, message, isGif);
    }

    // Add a client connection to a chat room group
    public async Task JoinChatRoom(string chatRoomId)
    {
        // Add the connection to the specified chat room group
        await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId);

        // Optionally send a system message to other users in the chat room
        await Clients.Group(chatRoomId).SendAsync("ReceiveMessage", "System", $"{Context.ConnectionId} joined the chat room.");
    }

    // Remove a client connection from a chat room group
    public async Task LeaveChatRoom(string chatRoomId)
    {
        // Remove the connection from the specified chat room group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId);

        // Optionally send a system message to other users in the chat room
        await Clients.Group(chatRoomId).SendAsync("ReceiveMessage", "System", $"{Context.ConnectionId} left the chat room.");
    }

    public override async Task OnConnectedAsync()
    {
        // Extract user ID from claims
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            Connections[Context.ConnectionId] = userId;
            Console.WriteLine($"User {userId} connected with ConnectionId: {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }
         
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryGetValue(Context.ConnectionId, out var userId))
        {
            Connections.Remove(Context.ConnectionId);
            Console.WriteLine($"User {userId} disconnected with ConnectionId: {Context.ConnectionId}");
        }

        await base.OnDisconnectedAsync(exception);
    }

}