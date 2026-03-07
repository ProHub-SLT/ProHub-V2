using System.ComponentModel.DataAnnotations;

namespace PROHUB.Models
{
    public class InternalIssue
    {
        public int Id { get; set; }

        [Display(Name = "Issue ID")]
        public string FormattedId => $"I{Id:D6}";

        [Required(ErrorMessage = "Issue Start Time is required")]
        [Display(Name = "Issue Start Time")]
        public DateTime? IssueStartTime { get; set; }

        [Required(ErrorMessage = "Internal Application is required")]
        [Display(Name = "Internal Application")]
        public int? InternalAppId { get; set; }

        [Required(ErrorMessage = "Reported By is required")]
        [Display(Name = "Reported By")]
        public string? ReportedBy { get; set; }

        [Display(Name = "Contact No")]
        public string? ReportingPersonContactNo { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Criticality is required")]
        [Display(Name = "Criticality")]
        public string? Criticality { get; set; }

        [Required(ErrorMessage = "Entered By is required")]
        [Display(Name = "Entered By")]
        public int? EnteredBy { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedTo { get; set; }

        [Display(Name = "Assigned By")]
        public int? AssignedBy { get; set; }

        [Display(Name = "Assigned Time")]
        public DateTime? AssignedTime { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "Issue Closed Time")]
        public DateTime? IssueClosedTime { get; set; }

        [Display(Name = "Action Taken")]
        public string? ActionTaken { get; set; }

        [Display(Name = "Entered Time")]
        public DateTime? EnteredTime { get; set; }

        // --- Properties for JOINed data (for display) ---

        [Display(Name = "Application Name")]
        public string? InternalAppName { get; set; } // From Internal_Platforms

        [Display(Name = "Entered By Name")]
        public string? EnteredByName { get; set; } // From Employee

        [Display(Name = "Assigned To Name")]
        public string? AssignedToName { get; set; } // From Employee

        [Display(Name = "Assigned By Name")]
        public string? AssignedByName { get; set; } // From Employee
    }
}