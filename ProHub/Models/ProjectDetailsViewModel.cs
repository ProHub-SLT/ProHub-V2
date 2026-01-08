using System;
using System.Collections.Generic;

namespace ProHub.Models
{
    public class ProjectDetailsViewModel
    {
        // Plan Info (Common)
        public int PlanId { get; set; }
        public DateTime? WeekStart { get; set; }
        public DateTime? WeekEnd { get; set; }
        public string UpdatedBy { get; set; }
        public string WorkPlanDesc { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // List of projects included in this plan record
        public List<ProjectDetailItem> Projects { get; set; } = new List<ProjectDetailItem>();
    }

    public class ProjectDetailItem
    {
        public string ApplicationName { get; set; }
        public string ProjectType { get; set; } // "External Solution" or "Internal Solution"
        public string DevelopedBy { get; set; }
        public string DevelopedTeam { get; set; }
        public string BackupPerson1 { get; set; }
        public string BackupPerson2 { get; set; }
        
        public DateTime? StartDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public string SDLCPhase { get; set; }
        public decimal? PercentageDone { get; set; }
        
        public string IntegratedApps { get; set; }
        public string BitBucketRepo { get; set; }
        public string DRAvailability { get; set; }
        public string HostedServerIP { get; set; } // AppIP
        public string ApplicationURL { get; set; }
        
        public string BusinessOwner { get; set; } 
        public string EndUsers { get; set; } 
        public string UserSpecificSection { get; set; } // Internal only
        
        public DateTime? UATDate { get; set; }
        public DateTime? VADate { get; set; }
        public DateTime? LaunchedDate { get; set; }
        
        public string ExposedThroughWAF { get; set; } 
        public decimal? SolutionValue { get; set; } 
        public string SupportAvailability { get; set; } 
    }
}
