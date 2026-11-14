using aspnetproject.Data;
using aspnetproject.Extensions;
using aspnetproject.Hubs;
using Microsoft.EntityFrameworkCore;

namespace aspnetproject;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            
            builder.Services.AddSwaggerConfiguration();
            builder.Services.AddOpenApi();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddJwtAuthentication(builder.Configuration);
            builder.Services.AddAuthorization();

            builder.Services.AddApplicationRateLimiting();
            builder.Services.AddCorsPolicies(builder.Configuration);
            
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddApplicationServices(builder.Configuration);
        }
        
        var app = builder.Build();
        {
            if (app.Environment.IsDevelopment())
            {
                // app.TestSeedEndpoint();
                
                app.MapSwagger();
                app.MapSwaggerUI();
                app.MapOpenApi();
            }

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseImageStorage();
            app.UseRouting();
            
            app.UseCors("OriginPolicy");

            // The rate limiter sits between them: it needs the caller read from the access token to count per user,
            // and must still reject over-limit requests before authorization runs.
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<MessageHub>("hubs/messages");

            app.Run();
        }
    }
}