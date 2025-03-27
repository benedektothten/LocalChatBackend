using Azure.Messaging.ServiceBus;
using Common.DTOs;
using Common.Persistence;
using Common.Settings;
using Microsoft.Extensions.Options;

namespace MessageProcessorService.Processors;

public class MessageQueueProcessor(ServiceBusClient client, IServiceProvider serviceProvider, IOptions<AzureServiceBusSettings> settings, ILogger<MessageQueueProcessor> logger)
    : BackgroundService
{
    private readonly ServiceBusProcessor _processor = client.CreateProcessor(settings.Value.QueueName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        var message = System.Text.Json.JsonSerializer.Deserialize<QueueMessages>(messageBody);
        
        logger.LogInformation($"Message received: {message?.Content}");

        try
        {
            // Create a new scope for the DbContext
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            if (message != null)
            {
                var newMessage = new MessageEntity
                {
                    ChatRoomId = message.ChatRoomId,
                    Content = message.Content,
                    SenderId = message.SenderId,
                    SentAt = message.SentAt,
                    UniqueId = message.UniqueId,
                    IsGif = message.IsGif
                };
                dbContext.Messages.Add(newMessage);
            }

            await dbContext.SaveChangesAsync();
            
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.LogError(e, "Error processing message");
            //await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Error processing message");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}