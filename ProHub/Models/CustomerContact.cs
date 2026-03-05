using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class CustomerContact
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        public int? Platform_ID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(10, ErrorMessage = "Title cannot exceed 10 characters")]
        public string? Customer_Title { get; set; }

        [Required(ErrorMessage = "Contact Person's Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\.\-]+$", ErrorMessage = "Name can only contain letters, spaces, dots, and hyphens")]
        public string? Contact_Name { get; set; }

        [Required(ErrorMessage = "Primary Phone is required")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain numbers")]
        public string? Contact_Phone1 { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "Phone number must be exactly 10 digits")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain numbers")]
        public string? Contact_Phone2 { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        public string? Contact_Email { get; set; }

        [StringLength(100, ErrorMessage = "Designation cannot exceed 100 characters")]
        public string? Contact_Designation { get; set; }

        [Required(ErrorMessage = "Company is required")]
        public int? Contact_Company { get; set; }

        // Navigation properties (optional)
        public ExternalPlatform? Platform { get; set; }
        public Company? Company { get; set; }
    }
}
