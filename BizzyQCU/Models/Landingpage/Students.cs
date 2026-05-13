using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizzyQCU.Models.Landingpage
{
    [Table("students")]
    public class Students
    {
        [Key]
        [Column("student_id")]
        public int StudentId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("firstname")]
        [StringLength(100)]
        public string Firstname { get; set; }

        [Column("lastname")]
        [StringLength(100)]
        public string Lastname { get; set; }

        [Required]
        [Column("student_number")]
        [StringLength(50)]
        public string StudentNumber { get; set; }

        [Column("section")]
        [StringLength(50)]
        public string Section { get; set; }

        [Column("contact_number")]
        [StringLength(20)]
        public string ContactNumber { get; set; }

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}