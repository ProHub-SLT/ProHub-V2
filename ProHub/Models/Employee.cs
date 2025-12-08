using System;
using System.Collections.Generic;

namespace ProHub.Models
{
    public class Employee
    {
        public int EmpId { get; set; }
        public string EmpName { get; set; }
        public string EmpEmail { get; set; }
        public string EmpPhone { get; set; }
        public DateTime? LastSuccessfulLogin { get; set; }
        public bool Locked { get; set; }
        public int? GroupID { get; set; }
        public EmpGroup Group { get; set; }
        public DateTime? DOB { get; set; }
        public string CallingName { get; set; }
        public string Gender { get; set; }
        public string Section { get; set; }

        // Navigation for ExternalPlatform
        public ICollection<ExternalPlatform> DevelopedPlatforms { get; set; }
        public ICollection<ExternalPlatform> Backup1Platforms { get; set; }
        public ICollection<ExternalPlatform> Backup2Platforms { get; set; }


        // Navigation for InternalPlatform
        public ICollection<InternalPlatform> DevelopedInternalPlatforms { get; set; }
        public ICollection<InternalPlatform> Backup1InternalPlatforms { get; set; }
        public ICollection<InternalPlatform> Backup2InternalPlatforms { get; set; }
    }
}
