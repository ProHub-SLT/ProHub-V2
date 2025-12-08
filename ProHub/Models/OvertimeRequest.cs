// File: Models/OvertimeRequest.cs
using System;

namespace ProHub.Models
{
    public class OvertimeRequest
    {
        public int ID { get; set; }
        public DateTime? Created_Date { get; set; }
        public int? Created_By { get; set; }
        public DateTime? Date { get; set; }
        public decimal? No_Of_Hours { get; set; }
        public string? Work_Description { get; set; }
        public int? Approval_For { get; set; }
        public string? Comment { get; set; }
        public int? Approved_By { get; set; }
        public DateTime? Approved_Date { get; set; }

        // View Helpers
        public string? CreatedByName { get; set; }
        public string? ApprovalForName { get; set; }
        public string? ApprovedByName { get; set; }
    }
}