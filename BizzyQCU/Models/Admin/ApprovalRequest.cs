using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizzyQCU.Models.Admin
{
    [Table("approval_requests")]
    public class ApprovalRequests
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; }

        [Column("password")]
        [StringLength(255)]
        public string Password { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Column("role")]
        public string Role { get; set; }

        // Student fields
        [Column("firstname")]
        [StringLength(100)]
        public string Firstname { get; set; }

        [Column("lastname")]
        [StringLength(100)]
        public string Lastname { get; set; }

        [Column("birthdate")]
        public DateTime? Birthdate { get; set; }

        [Column("student_number")]
        [StringLength(50)]
        public string StudentNumber { get; set; }

        [Column("section")]
        [StringLength(50)]
        public string Section { get; set; }

        [Column("contact_number")]
        [StringLength(20)]
        public string ContactNumber { get; set; }

        [Column("qcu_id")]
        public byte[] QcuId { get; set; }

        // Enterprise fields
        [Column("store_name")]
        [StringLength(255)]
        public string StoreName { get; set; }

        [Column("store_description")]
        public string StoreDescription { get; set; }

        [Column("gcash_number")]
        [StringLength(15)]
        public string GcashNumber { get; set; }

        [Column("uploaded_document")]
        public byte[] UploadedDocument { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        public bool? IsAccountEnabled { get; set; }

        // Computed property for display
        public string FullName
        {
            get
            {
                if (Role == "student")
                    return $"{Firstname} {Lastname}";
                else
                    return StoreName;
            }
        }
    }
}
