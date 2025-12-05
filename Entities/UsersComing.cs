using System.Text.Json.Nodes;

namespace Orch_back_API.Entities
{
    public class UsersComing
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Region { get; set; }
        public int? Age { get; set; }
        public string? City { get; set; }
        public string? Gender { get; set; }
      
        public IFormFile? ProfilePhoto { get; set; }
        public List<Messages> Messes { get; set; } = new List<Messages>();
        public List<Notifications> Notifications { get; set; } = new List<Notifications>();
    }
}
