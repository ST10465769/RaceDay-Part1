using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Connect to SQL Server using the connection string in appsettings.json
            builder.Services.AddDbContext<RaceDayDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // The controller asks for IPasswordHasher and gets my PBKDF2 PasswordHasher
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

            // Sessions keep the logged-in UserId and Role on the server.
            // The browser only gets a session cookie, not the data itself.
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            builder.Services.AddControllers();

            // Swagger, with annotations turned on so I can describe each endpoint
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options => options.EnableAnnotations());

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // UseSession must come before MapControllers so controllers can read the session
            app.UseSession();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

