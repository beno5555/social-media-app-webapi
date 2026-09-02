using aspnetproject.Data.Repositories;
using aspnetproject.Infrastructure.Dtos.Auth.Email;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using aspnetproject.Infrastructure.Services.Helpers;
using aspnetproject.Infrastructure.Services.Logging;
using aspnetproject.Infrastructure.Services.Websockets;

namespace aspnetproject.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddSingleton<UserConnectionTracker>();
        
        services.AddScoped<CommentRepository>();
        services.AddScoped<FriendshipRepository>();
        services.AddScoped<MessageRepository>();
        services.AddScoped<PostRepository>();
        services.AddScoped<UserRepository>();
        services.AddScoped<RoleRepository>();
        services.AddScoped<UserRoleRepository>();
        services.AddScoped<RefreshTokenRepository>();
        
        services.AddScoped<UserService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AccountSecurityService>();
        services.AddScoped<CommentService>();
        services.AddScoped<FriendshipService>();
        services.AddScoped<MessageService>();
        services.AddScoped<PostService>();
        services.AddScoped<PresenceService>();
        services.AddScoped<LogService>();
        
        services.AddScoped<PasswordHasher>();
        services.AddScoped<TokenGenerator>();

        services.AddScoped<SystemLogger>();
        services.AddScoped<DatabaseLogger>();

        services.Configure<EmailConfiguration>(configuration.GetSection("EmailConfiguration"));
        services.AddTransient<EmailSender>();

        return services;
    }
}