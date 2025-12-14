using System;
using System.ComponentModel.DataAnnotations;
using MyTraceCare.Models;

namespace MyTraceCare.ViewModels
{
    public class AdminUserViewModel
    {
        public string? Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public DateTime DOB { get; set; }

        // nullable ONLY for edit
        public string? Password { get; set; }
    }
}
