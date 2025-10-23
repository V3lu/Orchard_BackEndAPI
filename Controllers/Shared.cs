using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orch_back_API.Entities;
using System.Drawing.Imaging;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Orch_back_API.Controllers
{
    public class Shared
    {
        public static string ImgagesFolderPath = "C:\\Studia\\OrchardUserImages";
        private readonly MyJDBContext _context;
        private readonly IConfiguration _configuration;
        private static string _secret = Environment.GetEnvironmentVariable("SECRET");
        public Shared(MyJDBContext _context, IConfiguration _configuration)
        {
            this._configuration = _configuration;
            this._context = _context;
        }
        public string GenerateToken(Users user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Username),
                new Claim(ClaimTypes.Role,user.Role)
            };
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        public async Task<Users> Authenticate(Users userLogin)
        {
            PasswordHasher<Users> passwordHasher = new();
            var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() ==
                userLogin.Email.ToLower());
            if(currentUser != null)
            {
                if (passwordHasher.VerifyHashedPassword(userLogin, currentUser.Password, userLogin.Password) == PasswordVerificationResult.Success)
                {
                    return currentUser;
                }
            }
            return null;
        }
    }
}
