using System;
using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class OtherSystemIssuesViewModel
    {
        public int OSystem_Issue_ID { get; set; }

        [Display(Name = "Issue Start Time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
        public DateTime? Issue_Start_Time { get; set; }

        [Display(Name = "System Name")]
        public string System_Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Issue Closed Time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
        public DateTime? Issue_Closed_Time { get; set; }

        [Display(Name = "Action Taken")]
        public string Action_Taken { get; set; } = string.Empty;

        [Display(Name = "Entered Time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}")]
        public DateTime? Entered_Time { get; set; }

        [Display(Name = "Reporting Person Contact")]
        public string Reporting_Person_ContactNo { get; set; } = string.Empty;

        [Display(Name = "Criticality")]
        public string Criticality { get; set; } = string.Empty;
    }
}