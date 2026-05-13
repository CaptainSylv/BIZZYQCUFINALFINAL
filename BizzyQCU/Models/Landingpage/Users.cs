using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizzyQCU.Models.Landingpage
{
    [Table("users")]
    public class Users
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [Column("password")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Column("email")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Column("role")]
        [StringLength(20)]
        public string Role { get; set; } = "student";

        [Column("is_approved")]
        public bool IsApproved { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}