
using System.ComponentModel.DataAnnotations;

namespace PROHUB.Models
{
    public class ExternalIssue
    {
        public int Id { get; set; }

        [Display(Name = "Issue ID")]
        public string FormattedId => $"E{Id:D6}";

        [Display(Name = "Issue Start Time")]
        public DateTime? IssueStartTime { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        [Display(Name = "Platform")]
        public int PlatformId { get; set; }

        [Display(Name = "Reported By")]
        public int? ReportedBy { get; set; }

        [Required(ErrorMessage = "Entered By is required")]
        [Display(Name = "Entered By")]
        public int? EnteredBy { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Criticality")]
        public string? Criticality { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedTo { get; set; }

        [Display(Name = "Assigned By")]
        public int? AssignedBy { get; set; }

        [Display(Name = "Assigned Time")]
        public DateTime? AssignedTime { get; set; }

        [Display(Name = "Status")]
        public string? Status { get; set; }

        [Display(Name = "Issue Closed Time")]
        public DateTime? IssueClosedTime { get; set; }

        [Display(Name = "Action Taken")]
        public string? ActionTaken { get; set; }

        public int? SLADuration { get; set; }
        public bool? SLAachieved { get; set; }

        [Display(Name = "Entered Time")]
        public DateTime? EnteredTime { get; set; }

        public int? CustomerContactId { get; set; }

        // --- Properties for JOINed data (for display) ---
        [Display(Name = "Platform Name")]
        public string? PlatformName { get; set; }

        [Display(Name = "Reported By Name")]
        public string? ReportedByName { get; set; } // From CustomerContacts

        [Display(Name = "Entered By Name")]
        public string? EnteredByName { get; set; } // From Login 

        [Display(Name = "Assigned To Name")]
        public string? AssignedToName { get; set; } // From Employee

        [Display(Name = "Assigned By Name")]
        public string? AssignedByName { get; set; } // From Employee   
    }
}