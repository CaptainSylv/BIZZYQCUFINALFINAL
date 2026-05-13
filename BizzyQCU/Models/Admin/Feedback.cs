using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizzyQCU.Models.Admin
{
    [Table("feedbacks")]
    public class Feedback
    {
        [Key]
        [Column("feedback_id")]
        public int FeedbackId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("user_type")]
        public string UserType { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("contact_number")]
        public string ContactNumber { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("rating")]
        public int Rating { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
