using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BizzyQCU.Models.Landingpage
{
    [Table("enterprises")]
    public class Enterprises
    {
        [Key]
        [Column("enterprise_id")]
        public int EnterpriseId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("store_name")]
        [StringLength(255)]
        public string StoreName { get; set; }

        [Column("store_description")]
        public string StoreDescription { get; set; }

        [Column("store_logo")]
        [StringLength(500)]
        public string StoreLogo { get; set; }

        [Column("contact_number")]
        [StringLength(20)]
        public string ContactNumber { get; set; }

        [Column("gcash_number")]
        [StringLength(15)]
        public string GcashNumber { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "pending";

        [ForeignKey("UserId")]
        public virtual Users User { get; set; }
    }
}
