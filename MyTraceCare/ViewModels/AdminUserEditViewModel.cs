using System;
using MyTraceCare.Models;
using System.ComponentModel.DataAnnotations;

namespace MyTraceCare.ViewModels
{
    public class AdminUserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public Gender Gender { get; set; }

        public DateTime DOB { get; set; }

        // Optional password update
        public string? NewPassword { get; set; }
    }
}
