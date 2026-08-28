using System.Text;
using aspnetproject.BusinessLogic.Mappers;
using aspnetproject.BusinessLogic.Services;
using aspnetproject.BusinessLogic.Services.Helpers;
using aspnetproject.BusinessLogic.Services.Logging;
using aspnetproject.Data;
using aspnetproject.Data.Repositories;
using aspnetproject.Hubs;
using aspnetproject.Infrastructure.Mappers;
using aspnetproject.Infrastructure.Services.BusinessLogic;
using aspnetproject.Infrastructure.Services.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;

namespace aspnetproject;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
        });

        builder.Services.AddControllers();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Standard authorization header using bearer scheme. Example: \"bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
            
            options.OperationFilter<SecurityRequirementsOperationFilter>();
        });
        builder.Services.AddOpenApi();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var     jwtSection = builder.Configuration.GetSection("Jwt");
                var signInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signInKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path        = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        
        builder.Services.AddAuthorization();

        builder.Services.AddSignalR();

        builder.Services.AddCors(options =>
        {
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            options.AddPolicy("SignalRTestPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
        
        builder.Services.AddScoped<CommentRepository>();
        builder.Services.AddScoped<FriendshipRepository>();
        builder.Services.AddScoped<MessageRepository>();
        builder.Services.AddScoped<PostRepository>();
        builder.Services.AddScoped<UserRepository>();
        builder.Services.AddScoped<RefreshTokenRepository>();
        
        builder.Services.AddScoped<CommentMapper>();
        builder.Services.AddScoped<MessageMapper>();
        builder.Services.AddScoped<PostMapper>();
        builder.Services.AddScoped<UserMapper>();
        builder.Services.AddScoped<AuthMapper>();
        builder.Services.AddScoped<FriendshipMapper>();
        
        builder.Services.AddScoped<AccountService>();
        builder.Services.AddScoped<CommentService>();
        builder.Services.AddScoped<FriendshipService>();
        builder.Services.AddScoped<MessageService>();
        builder.Services.AddScoped<PostService>();
        
        builder.Services.AddScoped<AuthService>();
        
        builder.Services.AddScoped<PasswordHasher>();
        builder.Services.AddScoped<TokenGenerator>();

        builder.Services.AddScoped<SystemLogger>();

        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapSwagger();
            app.MapSwaggerUI();
            app.MapOpenApi();
        }

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception        = exceptionFeature?.Error;

                if (exception is not null)
                {
                    var sysLogger = context.RequestServices.GetRequiredService<SystemLogger>();
                    sysLogger.LogError(exception, context.Request.Method, context.Request.Path);

                    if (exception is DbUpdateException)
                    {
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    }

                    context.Response.ContentType = "application/json";

                    var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (environment.IsDevelopment())
                    {
                        var body = new
                        {
                            error = exception.Message,
                            stackTrace = exception.StackTrace
                        };

                        await context.Response.WriteAsJsonAsync(body);
                    }
                    else
                    {
                        await context.Response.WriteAsync("{\"error\":\"An unexpected error occurred.\"}");
                    }
                }
            });
        });

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseCors("SignalRTestPolicy");

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.MapHub<MessageHub>("hubs/messages");

        app.Run();
    }
}