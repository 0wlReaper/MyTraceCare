using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace MyTraceCare.Models
{
    public class PatientComment
    {
        public int Id { get; set; }

        // User who wrote the comment
        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        // Optional: Comment linked to a specific alert
        public int? AlertId { get; set; }
        public Alert? Alert { get; set; }

        // Optional: Parent comment (threaded replies)
        public int? ParentCommentId { get; set; }
        public PatientComment? ParentComment { get; set; }

        public List<PatientComment> Replies { get; set; } = new();

        // Optional: comment on a specific dataset day
        public DateTime? DataDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        public bool IsClinician { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
