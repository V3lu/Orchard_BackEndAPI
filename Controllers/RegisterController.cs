using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orch_back_API.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing;

namespace Orch_back_API.Controllers
{
    
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly MyJDBContext _dbcontext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegisterController> _logger;

        public RegisterController(
            MyJDBContext myJDBContext, 
            IConfiguration configuration,
            ILogger<RegisterController> _logger
           )
        {
            this._dbcontext = myJDBContext;
            this._configuration = configuration;
            this._logger = _logger;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] Users userCame)
        {
            // Log the registration attempt
            _logger.LogInformation("Registration attempt for user: {Username}, Email: {Gender}", 
                userCame.Username, userCame.Gender);
            
      
            try
            {
                PasswordHasher<Users> passwordHasher = new();
                Users userToAdd = userCame;
        
_logger.LogInformation("Starting user validation. User: {User}", 
    System.Text.Json.JsonSerializer.Serialize(userCame));
                _logger.LogDebug("Starting user validation check");

                // Check for existing users
                var users = await _dbcontext.Users.ToListAsync();
                
                foreach(var user in users)
                {
                    if(user.Username == userToAdd.Username)
                    {
                        _logger.LogWarning("Registration failed - Username already exists: {Username}", 
                            userToAdd.Username);
                        return Conflict(new { 
                            message = "Username already exists",
                            field = "username"
                        });
                    }
                    
                    if(user.Email == userToAdd.Email)
                    {
                        _logger.LogWarning("Registration failed - Email already exists: {Email}", 
                            userToAdd.Email);
                        return Conflict(new { 
                            message = "Email already exists",
                            field = "email"
                        });
                    }
                }

                _logger.LogDebug("User validation passed. Preparing to create user.");

                // Set user properties
                userToAdd.Role = "NFUA";
                userToAdd.Id = Guid.NewGuid();
             //     if (userToAdd.Gender.HasValue && !Enum.IsDefined(typeof(Gender), userToAdd.Gender.Value))
            //  {
            //       _logger.LogWarning("Invalid gender enum value: {Value}", userToAdd.Gender.Value);
           //         userToAdd.Gender = null; // Set to null if invalid
           //       }
                userToAdd.Gender = userCame.Gender;
                _logger.LogInformation("Assigned new User ID: {UserId} and Role: {Role}", 
                    userToAdd.Id, userToAdd.Role);

                    _logger.LogInformation("Assigned new User ID: {UserId} and Role: {Gender}", 
                    userToAdd.Id,  userToAdd.Gender);

                // Hash password (log this action but not the password itself)
                _logger.LogInformation("Hashing password for user: {Username}", userToAdd.Username);
                userToAdd.Password = passwordHasher.HashPassword(userToAdd, userToAdd.Password);
                _logger.LogInformation("Password hashed successfully");

                // Set default profile picture
                userToAdd.ProfilePhotoPath = Shared.ImgagesFolderPath + "\\defaultProfilePicture.png";
                _logger.LogDebug("Default profile picture set: {ProfilePhotoPath}", 
                    userToAdd.ProfilePhotoPath);

                // Add user to database
                _logger.LogInformation("Adding user to database: {Username}", userToAdd.Gender);
                await _dbcontext.Users.AddAsync(userToAdd);
                
                _logger.LogDebug("Saving changes to database...");
                await _dbcontext.SaveChangesAsync();
                
                _logger.LogInformation("User registered successfully: {Username}, UserId: {UserId}", 
                    userToAdd.Username, userToAdd.Id);

                // Log successful registration
                bool finished = true;
                _logger.LogInformation("Registration completed successfully for user: {Username}", 
                    userToAdd.Username);

                return Ok(new { 
                    finished,
                    userId = userToAdd.Id,
                    username = userToAdd.Username,
                    message = "User registered successfully"
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Log database-specific errors
                _logger.LogError(dbEx, "Database error occurred during registration for {Username}. " +
                    "Inner Exception: {InnerExceptionMessage}", 
                    userCame.Username, 
                    dbEx.InnerException?.Message);
                
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Database error occurred", details = "Please try again later" });
            }
            catch (ArgumentException argEx)
            {
                // Log argument-related errors
                _logger.LogError(argEx, "Invalid argument in registration for {Username}", 
                    userCame.Username);
                
                return BadRequest(new { 
                    message = "Invalid data provided", 
                    details = argEx.Message 
                });
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Unexpected error occurred during registration for {Username}. " +
                    "Request details - Username: {Username}, Email: {Email}", 
                    userCame.Username, userCame.Username, userCame.Email);
                
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { 
                        message = "An unexpected error occurred", 
                        errorId = Guid.NewGuid().ToString() // For support reference
                    });
            }
            finally
            {
                // Optional: Log request completion
                _logger.LogDebug("Registration request processing completed");
            }
        }

        // Additional logging for performance monitoring
        private void LogPerformanceMetrics(string operation, TimeSpan elapsedTime)
        {
            if (elapsedTime.TotalSeconds > 5) // Warn if operation takes more than 5 seconds
            {
                _logger.LogWarning("Registration operation '{Operation}' took {ElapsedMs}ms which is above threshold",
                    operation, elapsedTime.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug("Registration operation '{Operation}' completed in {ElapsedMs}ms",
                    operation, elapsedTime.TotalMilliseconds);
            }
        }
    }
}