using Common.Configurations;
using LocalChat.Common.Persistence;
using MessageProcessorService.Processors;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddAzureServiceBus(builder.Configuration);
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DefaultConnection"]));

builder.Services.AddHostedService<MessageQueueProcessor>();

var host = builder.Build();

host.Run();