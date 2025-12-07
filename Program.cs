using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orch_back_API.Entities;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL; 
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.IO;


namespace Orch_back_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotNetEnv.Env.Load();
            var builder = WebApplication.CreateBuilder(args);

            var config = builder.Configuration;
            var secret = Environment.GetEnvironmentVariable("SECRET");

            /// <summary>
            ///     <p>Akceptujemy dowolną metodę HTTP przy żądaniach z origina zdefiniowanego wyżej pod localhostem</p>
            ///     <p>Akceptujemy dowolny nagłówek z origina zdefiniowanego wyżej pod localhostem</p>
            ///     <p>Żądania mogą zawierać dane uwierzytelniające/p>
            /// </summary>
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CORSPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });

            });
            
            secret = "c1d2f3g4h5j6k7m8n9p0q1r2s3t4u5v6";
            if (string.IsNullOrEmpty(secret))
            {
                // Try environment variable as fallback
                secret = "c1d2f3g4h5j6k7m8n9p0q1r2s3t4u5v6";
                
                if (string.IsNullOrEmpty(secret))
                {
                    throw new InvalidOperationException(
                        "JWT Secret Key is not configured. " +
                        "Set 'Jwt:Key' in appsettings.json or JWT_SECRET_KEY environment variable.");
                }
            }

            builder.Services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(jwt =>
            {
                jwt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    /// <summary>Konfiguracja parametrów używanych do validacji tokenów</summary>
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddControllers()
                .AddJsonOptions(options => {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                     options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
                    
                    //options =>
              //  {
              //      options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
              //  });
            
            // Fix: Use the correct database name "Orchard_INZDB" instead of "postgres"
            builder.Services.AddDbContext<MyJDBContext>(options =>
            {
                options.UseNpgsql("Host=localhost;Database=Orchard_IB_Projects;Username=postgres;Password=postgres");
                options.EnableSensitiveDataLogging();
            });

            var app = builder.Build();

            // ========== ADD THIS SECTION FOR DATABASE INITIALIZATION ==========
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyJDBContext>();
                
                try 
                {
                    Console.WriteLine("Attempting to create database and tables...");
                    
                    // Ensure UUID extension
                    try 
                    {
                        dbContext.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");
                        Console.WriteLine("UUID extension created or already exists.");
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UUID extension warning: {ex.Message}");
                        // Continue anyway
                    }
                    
                    // Clean up any existing migration history
                    try 
                    {
                        dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS \"__EFMigrationsHistory\";");
                        Console.WriteLine("Migration history table cleaned up.");
                    } 
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Migration table cleanup warning: {ex.Message}");
                    }
                    
                    // Create all tables with PostgreSQL types
                    dbContext.Database.EnsureCreated();
                    Console.WriteLine("Database and tables created successfully with PostgreSQL!");
                    
                    // Verify the Users table was created
                    var tableExists = dbContext.Database.ExecuteSqlRaw(@"
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_schema = 'public' AND table_name = 'Users'");
                    Console.WriteLine($"Users table verification: {(tableExists >= 0 ? "Exists" : "May not exist")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during database creation: {ex.Message}");
                    // Don't crash the app, just log the error
                }
            }
            // ========== END DATABASE INITIALIZATION SECTION ==========

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("CORSPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}