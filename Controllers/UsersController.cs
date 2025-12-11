using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Validations;
using Orch_back_API.Entities;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orch_back_API.Controllers
{
    public class NotificationObjectFromApi
    {
        public string VisitorId { get; set; }
        public string HostId { get; set;}
    }

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly MyJDBContext _context;
        public UsersController(MyJDBContext context)
        {
            this._context = context;
        }

        [HttpPost]
        [Route("updatedata")]
        public async Task<IActionResult> UpdateUserData([FromForm] UsersComing user)
        {
            PasswordHasher<Users> passwordHasher = new();
            var coPrzyszlo = user;
            Users userConverted = new Users();
            if (coPrzyszlo.ProfilePhoto == null)
            {
                var userCame = await _context.Users.Where(eb => eb.Id == user.Id).FirstOrDefaultAsync();
                userConverted = new Users
                {
                    Id = coPrzyszlo.Id,
                    Username = coPrzyszlo.Username,
                    Password = coPrzyszlo.Password,
                    Email = coPrzyszlo.Email,
                    Role = coPrzyszlo.Role,
                    Region = coPrzyszlo.Region,
                    Age = coPrzyszlo.Age,
                    City = coPrzyszlo.City,
                    ProfilePhotoPath = userCame!.ProfilePhotoPath,
                    Notifications = coPrzyszlo.Notifications,
                    Messes = coPrzyszlo.Messes
                };
            }
            else
            {
                userConverted = new Users
                {
                    Id = coPrzyszlo.Id,
                    Username = coPrzyszlo.Username,
                    Password = coPrzyszlo.Password,
                    Email = coPrzyszlo.Email,
                    Role = coPrzyszlo.Role,
                    Region = coPrzyszlo.Region,
                    Age = coPrzyszlo.Age,
                    City = coPrzyszlo.City,
                    ProfilePhotoPath = Shared.ImgagesFolderPath + "\\" + coPrzyszlo.Id.ToString() + "ProfilePhoto" + coPrzyszlo.ProfilePhoto.FileName.ToString(),
                    Notifications = coPrzyszlo.Notifications,
                    Messes = coPrzyszlo.Messes
                };
            }
            userConverted.Password = passwordHasher.HashPassword(userConverted, userConverted.Password);
            if(coPrzyszlo.ProfilePhoto != null)
            {
                using (Stream fileStream = new FileStream(userConverted.ProfilePhotoPath, FileMode.Create))
                {
                    coPrzyszlo.ProfilePhoto.CopyTo(fileStream);  
                    fileStream.Dispose();
                }
            }

            await _context.Users.Where(eb => eb.Id == userConverted.Id).ExecuteUpdateAsync(setters => setters
                .SetProperty(eb => eb.Id, userConverted.Id).SetProperty(eb => eb.Username, userConverted.Username)
                .SetProperty(eb => eb.Password, userConverted.Password).SetProperty(eb => eb.Email, userConverted.Email)
                .SetProperty(eb => eb.Role, userConverted.Role).SetProperty(eb => eb.Region, userConverted.Region)
                .SetProperty(eb => eb.Age, userConverted.Age).SetProperty(eb => eb.City, userConverted.City)
                .SetProperty(eb => eb.ProfilePhotoPath, userConverted.ProfilePhotoPath)); 
            _context.ChangeTracker.Clear();
            return Ok(userConverted);
        }

        [HttpPost]
        [Route("getuserphoto")]
        public async Task<IActionResult> GetUserImage([FromBody] Users userWithIdOnly)
        {
            var user = await _context.Users.Where(aw => aw.Id == userWithIdOnly.Id).FirstAsync();
            var filePath = user.ProfilePhotoPath;

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var image = System.IO.File.OpenRead(filePath);
            string extension = Path.GetExtension(image.Name);
            return File(image, "image/" + extension.ToString().Substring(1));
        }

        [HttpPost]
        [Route("getuserbyid")]
        public async Task<IActionResult> GetUserById([FromBody] UsersComing user)
        {
            var id = user.Id;
            var userToReturn = await _context.Users.Where(eb => eb.Id.Equals(id)).FirstOrDefaultAsync();
            if(userToReturn != null)
            {
                return Ok(new { userToReturn });
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Route("nearby-users")]
        public async Task<IActionResult> GetNearbyUsers([FromBody] UsersComing currentUser)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(currentUser.City))
            {
                query = query.Where(u => u.City == currentUser.City);
            }

            if (!string.IsNullOrEmpty(currentUser.Region))
            {
                query = query.Where(u => u.Region == currentUser.Region);
            }

            var users = await query
                .OrderBy(u => u.Username) 
                .Take(5)
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        [Route("addnotificationwhenprofilevisited")]
        public async Task<IActionResult> AddNotificationWhenProfileVisited([FromBody] NotificationObjectFromApi fromApi)
        {
            Notifications notificationToBeAddedToDatabase = new Notifications();
            Users authorOf = await _context.Users.Where(eb => eb.Id.ToString() == fromApi.VisitorId).FirstOrDefaultAsync();
            notificationToBeAddedToDatabase.Id = Guid.NewGuid();
            notificationToBeAddedToDatabase.Author = authorOf;
            notificationToBeAddedToDatabase.AuthorId = authorOf.Id;
            notificationToBeAddedToDatabase.Content = "User " + authorOf.Username + " has visited your profile";
            notificationToBeAddedToDatabase.SendDate = DateTime.Now;
            notificationToBeAddedToDatabase.DeliveryId = new Guid(fromApi.HostId);
            await _context.Notifications.AddAsync(notificationToBeAddedToDatabase);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [Route("getuserssearchedforwithfilters")]
        public async Task<IActionResult> GetUsersSearchedForWithFilters([FromForm] UsersComing user)  
        {
            var userCame = user;

            if (!IsRegion(userCame) && !IsAge(userCame) && !IsCity(userCame))
            {
                return NotFound();
            }

            var query = _context.Users.AsQueryable();

            if (IsRegion(userCame))
            {
                query = query.Where(eb => eb.Region == userCame.Region);
            }

            if (IsAge(userCame))
            {
                query = query.Where(eb => eb.Age == userCame.Age);
            }

            if (IsCity(userCame))
            {
                query = query.Where(eb => eb.City == userCame.City);
            }

            var users = await query.ToListAsync();
            var userToRemove = await _context.Users.Where(eb => eb.Id == userCame.Id).FirstOrDefaultAsync();
            users.Remove(userToRemove!);
            return Ok(new {users});
        }

        [HttpPost]
        [Route("checkifusernameexists")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckIfUsernameExists([FromBody] UsersComing user)
        {
            var users = await _context.Users.ToListAsync();

            foreach(var userInLoop in users)
            {
                if(userInLoop.Username == user.Username)
                {
                    return Ok(true);
                }
            }
            return Ok(false);
        }

        [HttpPost]
        [Route("checkifemailexists")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckIfEmailExists([FromBody] UsersComing user)
        {
            var users = await _context.Users.ToListAsync();

            foreach (var userInLoop in users)
            {
                if (userInLoop.Email == user.Email)
                {
                    return Ok(true);
                }
            }
            return Ok(false);
        }

        private bool IsRegion(UsersComing user)
        {
            if(user.Region == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool IsAge(UsersComing user)
        {
            if (user.Age == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private bool IsCity(UsersComing user)
        {
            if (user.City == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

