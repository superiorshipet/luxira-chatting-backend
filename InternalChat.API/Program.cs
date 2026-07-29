using System.Text;
using InternalChat.Application.Interfaces.Repositories;
using InternalChat.Application.Interfaces.Services;
using InternalChat.Application.Services;
using InternalChat.Infrastructure.Identity;
using InternalChat.Infrastructure.Persistence;
using InternalChat.Infrastructure.Persistence.Repositories;
using InternalChat.Infrastructure.Storage;
using InternalChat.Infrastructure.Caching;
using InternalChat.Infrastructure.SignalR;
using InternalChat.API.Hubs;
using InternalChat.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // SignalR needs credentials
    });
});

// Configure Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost"));

// Configure DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupMemberRepository, GroupMemberRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageReadRepository, MessageReadRepository>();
builder.Services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddScoped<IUserQueryRepository, UserQueryRepository>();
builder.Services.AddScoped<IGroupQueryRepository, GroupQueryRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IChatNotifier, ChatNotifier>();
builder.Services.AddSingleton<IJwtProvider, JwtProvider>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
builder.Services.AddScoped<IFileStorage, InternalChat.Infrastructure.Storage.CloudinaryService>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Configure JWT Authentication
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "InternalChatSuperSecretKey_1234567890_PleaseChangeMe!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "InternalChat",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "InternalChatClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Allow SignalR to read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbSeeder.SeedAsync(context);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InternalChat API V1");
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
