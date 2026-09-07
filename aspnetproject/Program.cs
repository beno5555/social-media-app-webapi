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
            app.UseRouting();
            
            app.UseCors("OriginPolicy");
            app.UseRateLimiter();

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.MapHub<MessageHub>("hubs/messages");

            app.Run();
        }
    }
}