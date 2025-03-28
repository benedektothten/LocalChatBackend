using System.Text;
using Common.Caches;
using Common.Caches.Extensions;
using Common.Configurations;
using Common.Persistence;
using Common.Settings;
using LocalChat;
using LocalChat.Endpoints;
using LocalChat.Endpoints.HealthCheck;
using LocalChat.Services;
using LocalChat.Services.Extensions;
using LocalChat.Validators;
using LocalChat.Validators.Extensions;
using LocalChat.Validators.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer(); // For Minimal API Swagger
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); // For SignalR real-time communication
builder.Services.AddHealthChecks().AddCheck<DefaultHealthCheck>("Default health check");
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port)); // Listen on all IP addresses for the specified port
});

// Add Key Vault configuration
builder.Configuration.AddAzureKeyVaultConfiguration(builder.Configuration);
builder.Services.AddAzureServiceBus(builder.Configuration);
builder.Services.AddRedisCache(builder.Configuration);

builder.Services.AddCaches();
builder.Services.AddValidators();
builder.Services.AddServices();
Console.WriteLine($"Default connections: {builder.Configuration["DefaultConnection"]}");
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration["DefaultConnection"]));

builder.Services.Configure<JwtSecrets>(builder.Configuration);

//Add auth
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "your-issuer", // e.g., https://yourdomain.com
                ValidAudience = "your-audience", // e.g., https://yourdomain.com
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtTokenSecret"])) // Use a secure key
            };
            
            // Enable JWT authentication for SignalR connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    // If the request is for Hub and access token exists, set it in context.Token
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };

        }
    )
    .AddCookie() // Use Cookies for storing user session
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GoogleAuthClientId"];
        options.ClientSecret = builder.Configuration["GoogleAuthClientSecret"];

        // The default callback Google redirects to after authentication
        options.CallbackPath = "/signin-google";

        // Optional: Additional scopes (e.g., to fetch user profile data)
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.SaveTokens = true; // Save access and refresh tokens
    });

builder.Services.AddAuthorization();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Replace with your frontend's origin
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials() // Required for SignalR
            .SetIsOriginAllowed(origin => true); // OR specify allowed origins explicitly
    });

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Define SignalR Hub
app.MapHub<ChatHub>("/hubs/chat");

// Define Minimal API Endpoints
app.MapLoginEndpoints();
app.MapUserEndpoints();
app.MapMessageEndpoints();
app.MapChatRoomEndpoints();
app.MapHealthChecks("/healthz");

app.MapGet("/", () => Results.Ok("Healthy"));

app.Run();