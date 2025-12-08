using PROHUB.Models;
using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class Trainees
    {
        [Key]
        public int Trainee_ID { get; set; }

        public string FormattedTrainee_ID => $"T{Trainee_ID:D6}";

        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(@"^[A-Za-z.\s]+$", ErrorMessage = "Name can only contain letters, spaces and dots")]
        [Display(Name = "Trainee Name")]
        public string Trainee_Name { get; set; } = string.Empty; // <-- THIS IS THE FIX

        [Required(ErrorMessage = "Phone Number is required")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^(\d{10})$", ErrorMessage = "Invalid Phone Number. Enter 10 digits.")]
        public string? Trainee_Phone { get; set; }

        [Required(ErrorMessage = "NIC is required")]
        [RegularExpression(@"^([0-9]{9}[Vv]|[0-9]{12})$", ErrorMessage = "Invalid NIC format. Use 9 digits with V or 12 digits.")]
        [Display(Name = "NIC")]
        public string? Trainee_NIC { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Required(ErrorMessage = "Email Address is required")]
        [Display(Name = "Email Address")]
        public string? Trainee_Email { get; set; }

        [Required(ErrorMessage = "Home Address is required")]
        [Display(Name = "Home Address")]
        public string? Trainee_HomeAddress { get; set; }

        [Required(ErrorMessage = "Training Start Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Training Start Date")]
        public DateTime? Training_StartDate { get; set; }

        [Required(ErrorMessage = "Training End Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Training End Date")]
        public DateTime? Training_EndDate { get; set; }

        [Required(ErrorMessage = "Institute is required")]
        [Display(Name = "Institute")]
        public string? Institute { get; set; }

        [Required(ErrorMessage = "Languages is required")]
        [Display(Name = "Languages Known")]
        public string? Languages_Known { get; set; }

        [Required(ErrorMessage = "Field of Specialization is required")]
        [Display(Name = "Field of Specialization ID")]
        public int? field_of_spec_id { get; set; }

        [Required(ErrorMessage = "Supervisor is required")]
        [Display(Name = "Supervisor ID")]
        public int? Supervisor { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Target Date")]
        public DateTime? Target_Date { get; set; }

        [Display(Name = "Assigned Work Description")]
        public string? AssignedWork_Desc { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Start Date")]
        public DateTime? payment_start_date { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment End Date")]
        public DateTime? payment_end_date { get; set; }

        [Display(Name = "Requested Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? requested_payment_date { get; set; }

        [Display(Name = "Absent Count")]
        public int absent_Count { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Terminated Date")]
        public DateTime? terminated_date { get; set; }

        [Display(Name = "Termination Reason")]
        public string? terminated_reason { get; set; }

        // Optional navigation properties
        public string? SupervisorName { get; set; }  // From Employee table
        public string? FieldOfSpecName { get; set; } // From Fields_of_Specialization
    }
}
