using Microsoft.AspNetCore.Identity;

namespace MyTraceCare.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public Gender? Gender { get; set; }
        public DateTime? DOB { get; set; }

        public UserRole Role { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum Gender { Male, Female, Other, NonBinary }
    public enum UserRole { Patient, Clinician, Admin }
}
