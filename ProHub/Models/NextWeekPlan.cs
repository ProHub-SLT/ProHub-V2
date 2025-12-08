// File: Models/NextWeekPlan.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ProHub.Models
{
    [Table("WorkPlan")]
    public class NextWeekPlan
    {
        [Key]
        public int ID { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // These stay as INT FK columns in DB
        public int? ExternalPlatform { get; set; }
        public int? InternalApp { get; set; }

        [Column(TypeName = "TEXT")]
        public string? WorkPlanDesc { get; set; }

        public int? UpdatedBy { get; set; }
        public string UpdatedByName { get; set; } = "";

        public DateTime? UpdatedOn { get; set; }

        // Navigation properties for dropdowns
        [NotMapped]
        public List<ExternalPlatform> ExternalPlatforms { get; set; } = new();

        [NotMapped]
        public List<InternalPlatform> InternalPlatforms { get; set; } = new();

        // Selected project IDs (used in forms)
        [NotMapped]
        public List<int> ExternalProjectIds { get; set; } = new();

        [NotMapped]
        public List<int> InternalProjectIds { get; set; } = new();

        [NotMapped]
        public string WorkPlanDetails { get; set; } = "";
    }
}