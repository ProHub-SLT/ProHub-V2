using System.ComponentModel.DataAnnotations;

namespace PROHUB.Models
{
    public class Freelancer
    {
        public int FreelancerId { get; set; }

        [Display(Name = "Freelancer ID")]
        public string FormattedFreelancerId => $"F{FreelancerId:D6}";

        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(@"^[A-Za-z.\s]+$", ErrorMessage = "Name can only contain letters, spaces and dots")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "NIC is required")]
        [RegularExpression(@"^([0-9]{9}[Vv]|[0-9]{12})$", ErrorMessage = "Invalid NIC format. Use 9 digits with V or 12 digits.")]
        public string NIC { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;
        public string ProjectScope { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string BudgetAvailable { get; set; } = string.Empty;

        [Required(ErrorMessage = "Freelancer Start Date is required")]
        public DateOnly? StartDate { get; set; }

        [Required(ErrorMessage = "Freelancer End Date is required")]
        public DateOnly? EndDate { get; set; }


        public string Duration { get; set; } = string.Empty;

        public List<FreelancerTaskViewModel> Tasks { get; set; } = new();
    }

    public class FreelancerTaskViewModel
    {
        public int TaskId { get; set; }
        public int ID { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        public string Payment { get; set; } = string.Empty;
        public DateOnly? DeliveryDueDate { get; set; }
        public string Status { get; set; } = "Pending";
        public string Paid { get; set; } = "No";
    }
}
