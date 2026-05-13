using System.ComponentModel.DataAnnotations;

namespace BizzyQCU.Models.Landingpage
{
    public class StudentRegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string Lastname { get; set; }

        [Required(ErrorMessage = "Student number is required.")]
        [StringLength(50)]
        public string StudentNumber { get; set; }

        [Required(ErrorMessage = "Section is required.")]
        [StringLength(50)]
        public string Section { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(20)]
        public string ContactNumber { get; set; }
    }

    public class EnterpriseRegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Store name is required.")]
        [StringLength(255)]
        public string StoreName { get; set; }

        [StringLength(500)]
        public string StoreDescription { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [StringLength(20)]
        public string ContactNumber { get; set; }

        [StringLength(15)]
        public string GcashNumber { get; set; }
    }
}