
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.EntityFrameworkCore;
using OrderManagementApp.Data;

namespace OrderManagementApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
            });

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                //This property is set to specify the connection string for Redis
                //The value is fetched from the application's configuration system, i.e., appsettings.json file
                options.Configuration = builder.Configuration["RedisCacheOptions:Configuration"];
                //This property helps in setting a logical name for the Redis cache instance. 
                //The value is also fetched from the appsettings.json file
                options.InstanceName = builder.Configuration["RedisCacheOptions:InstanceName"];
            });
            builder.Services.AddControllers();

            builder.Services.AddHttpClient();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
