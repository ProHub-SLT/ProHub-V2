using System;
using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class Partner
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "Organization name is required")]
        [Display(Name = "Organization Name")]
        public string Partner_Organization { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Contact Person's Title")]
        public string Partner_Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(@"^[A-Za-z.\s]+$", ErrorMessage = "Name can only contain letters, spaces and dots")]
        [Display(Name = "Contact Person's Name")]
        public string Partner_Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Primary phone is required")]
        [RegularExpression(@"^(\d{10})$", ErrorMessage = "Invalid Phone Number. Enter 10 digits.")]
        [Display(Name = "Contact Person's Phone 1")]
        public string Partner_Phone1 { get; set; } = string.Empty;

        [RegularExpression(@"^(\d{10})$", ErrorMessage = "Invalid Phone Number. Enter 10 digits.")]
        [Display(Name = "Contact Person's Phone 2")]
        public string? Partner_Phone2 { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Contact Person's Email")]
        public string Partner_Email { get; set; } = string.Empty;

        [Display(Name = "Contact Person's Designation")]
        public string? Partner_Designation { get; set; }
    }
}
