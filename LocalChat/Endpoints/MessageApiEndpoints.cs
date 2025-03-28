using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Common.Caches;
using Common.DTOs;
using Common.Persistence;
using Common.Settings;
using LocalChat.DTOs;
using LocalChat.Endpoints.EndpointFilters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalChat.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/messages", async (ChatDbContext dbContext) =>
        {
            var messages = await dbContext.Messages
                .Include(m => m.ChatRoom)
                .Include(m => m.Sender)
                .Select(m => new MessageDto(
                    m.MessageId,
                    m.ChatRoomId,
                    m.SenderId,
                    m.Content,
                    m.SentAt,
                    m.Sender.Username, // Include only the Sender's username
                    m.IsGif ?? false
                ))
                .ToListAsync();

            return Results.Ok(messages);

        })
            .AddEndpointFilter<AuthenticationFilter>()
            .RequireAuthorization();
        
        routes.MapPost("/api/messages", async (CreateMessageRequest request, IHubContext<ChatHub> chatHubContext, ServiceBusClient serviceBusClient, IUserCache userCache, IOptions<AzureServiceBusSettings> options) =>
        {
            //Get userName
            var userName = await userCache.GetUserNameAsync(request.SenderId);
            var messageUniqueId = Guid.NewGuid();
            // Immediately notify all clients in the chat room using SignalR
            await chatHubContext.Clients.Group(request.ChatRoomId.ToString())
                .SendAsync("ReceiveMessage", messageUniqueId, request.ChatRoomId, request.SenderId, userName, request.Content, request.IsGif);
            
            // Prepare the message payload for the queue
            await PrepareAndSendMessageToQueue(request, messageUniqueId, serviceBusClient, options);

            // Return an accepted response indicating that the message is being processed
            return Results.Accepted();
        })
        .AddEndpointFilter<AuthenticationFilter>()
        .AddEndpointFilter<ValidateSenderFilter>()
        .RequireAuthorization();

        routes.MapGet("/api/messages/{id}", async (int id, ChatDbContext dbContext) =>
        {
            var message = await dbContext.Messages
                .Include(m => m.ChatRoom)
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            return message is not null ? Results.Ok(message) : Results.NotFound();
        })
        .AddEndpointFilter<AuthenticationFilter>()
        .RequireAuthorization();
    }

    private static async Task PrepareAndSendMessageToQueue(CreateMessageRequest request, Guid uniqueId, ServiceBusClient serviceBusClient, IOptions<AzureServiceBusSettings> options)
    {
        var message = new QueueMessages(request.ChatRoomId, uniqueId, request.SenderId ,request.Content, request.IsGif, DateTime.UtcNow);

        // Serialize the message to JSON
        var messageJson = JsonSerializer.Serialize(message);

        // Send the message to Azure Service Bus queue
        var sender = serviceBusClient.CreateSender(options.Value.QueueName);
        var serviceBusMessage = new ServiceBusMessage(messageJson);

        await sender.SendMessageAsync(serviceBusMessage);
    }
}