using Common.Persistence;
using LocalChat.DTOs;
using LocalChat.Endpoints.EndpointFilters;
using LocalChat.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LocalChat.Endpoints;

public static class ChatRoomEndpoints
{
    public static void MapChatRoomEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/chatrooms", async ([FromQuery] int senderId, ChatDbContext dbContext) =>
        {
            var chatRooms = await dbContext.ChatRooms
                .Where(c => c.UserChatRooms.Any(uc => uc.UserId == senderId)) // Only chatrooms the user is a member of
                .Select(c => new
                {
                    c.ChatRoomId,
                    c.Name,
                    c.IsPrivate,
                    c.CreatedAt,
                    LatestMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new
                        {
                            m.MessageId,
                            m.Content,
                            m.SentAt,
                            m.SenderId,
                            SenderUsername = m.Sender.Username, // Assuming navigation property is available
                            m.IsGif
                        })
                        .FirstOrDefault() // Get the latest message
                })
                .ToListAsync();

            return Results.Ok(chatRooms);
        })
        .AddEndpointFilter<AuthenticationFilter>()
        .RequireAuthorization();

        // Creates a new chatroom
        routes.MapPost("/api/chatrooms", async (CreateChatRoomRequest request, ChatDbContext dbContext, IHubContext<ChatHub> chatHubContext
            ) =>
        {
            var newChatRoom = new ChatRoomEntity
            {
                Name = request.Name,
                IsPrivate = request.IsPrivate,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.ChatRooms.Add(newChatRoom);
            await dbContext.SaveChangesAsync();
            foreach (var userId in request.membersToAdd)
            {
                dbContext.UserChatRooms.Add(new UserChatRoomEntity
                    { UserId = userId, ChatRoomId = newChatRoom.ChatRoomId });
            }
            
            // SignalR Notification: Notify users who are part of the new chatroom
            foreach (var userId in request.membersToAdd)
            {
                // Send a SignalR message to the client group corresponding to each user
                await chatHubContext.Clients.User(userId.ToString())
                    .SendAsync("ChatRoomCreated", new
                    {
                        newChatRoom.ChatRoomId,
                        newChatRoom.Name,
                        newChatRoom.IsPrivate,
                        newChatRoom.CreatedAt
                    });
            }

            await dbContext.SaveChangesAsync();
            return Results.Created($"/api/chatrooms/{newChatRoom.ChatRoomId}", new ChatRoomResponse(newChatRoom.ChatRoomId, newChatRoom.Name, newChatRoom.IsPrivate, newChatRoom.CreatedAt));
        })
            .AddEndpointFilter<AuthenticationFilter>()
            .RequireAuthorization();

        routes.MapGet("/api/chatrooms/{id}", async (int id, ChatDbContext dbContext) =>
        {
            var chatRoom = await dbContext.ChatRooms.FindAsync(id);
            return chatRoom is not null ? Results.Ok(chatRoom) : Results.NotFound();
        })
        .AddEndpointFilter<AuthenticationFilter>()
        .RequireAuthorization();;
        
        // Get messages in a chatroom and all the avatars for the users in it
        routes.MapGet("/api/chatrooms/{chatRoomId}/messages",
            async ([FromRoute] int chatRoomId, [FromQuery] int senderId, ChatDbContext dbContext) =>
        {
            // Fetch messages if user is authorized
            var messages = await dbContext.Messages
                .Where(m => m.ChatRoomId == chatRoomId)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(
                    m.MessageId,
                    m.ChatRoomId,
                    m.SenderId,
                    m.Content,
                    m.SentAt,
                    m.Sender.Username, // Resolve SenderUsername from navigation property
                    m.IsGif
                ))
                .ToListAsync();
            
            var senderAvatars = await dbContext.UserChatRooms.Include(c => c.User)
                .Where(c => c.ChatRoomId == chatRoomId)
                .Select(m => new { m.User.UserId, m.User.AvatarUrl })
                .ToListAsync();
            
            var response = new
            {
                Messages = messages,
                Avatars = senderAvatars
            };

            return Results.Ok(response);
        })
            .AddEndpointFilter<AuthenticationFilter>()
            .AddEndpointFilter<ValidateSenderFilter>()
            .RequireAuthorization();
    }
}