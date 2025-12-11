using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orch_back_API.Entities
{
    public class Users
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Region { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? City { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public List<Messages> Messes { get; set; } = new List<Messages>();
        public List<Notifications> Notifications { get; set; } = new List<Notifications>();

    }
}
