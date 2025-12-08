using System;

namespace ProHub.Models
{
    public class ExternalPlatform
    {
        public int Id { get; set; }
        public string PlatformName { get; set; }
        public string PlatformType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? TargetDate { get; set; }

        // Employee FKs
        public int? DevelopedById { get; set; }
        public Employee? DevelopedBy { get; set; }

        public string DevelopedTeam { get; set; }
        public string BitBucket { get; set; }
        public string BITBucketRepo { get; set; }

        // SDLC Phase FK
        public int? SDLCStageId { get; set; }
        public SDLCPhase? SDLCStage { get; set; }

        public decimal? PercentageDone { get; set; }
        public string Status { get; set; }
        public DateTime? StatusDate { get; set; }
        public string IntegratedApps { get; set; }
        public string DR { get; set; }

        // Company FK
        public int? CompanyId { get; set; }
        public Company? Company { get; set; }

        // Sales Team FK
        public int? SalesTeamId { get; set; }
        public SalesTeam? SalesTeam { get; set; }

        public string SalesAM { get; set; }
        public string SalesManager { get; set; }
        public string SalesEngineer { get; set; }
        public DateTime? UATDate { get; set; }
        public DateTime? VADate { get; set; }
        public DateTime? LaunchedDate { get; set; }
        public string PlatformOwner { get; set; }
        public string APP_Owner { get; set; }
        public decimal? PlatformOTC { get; set; }
        public decimal? PlatformMRC { get; set; }
        public string ContractPeriod { get; set; }
        public decimal? IncentiveEarned { get; set; }
        public decimal? IncentiveShare { get; set; }
        public DateTime? BillingDate { get; set; }
        public string ProposalUploaded { get; set; }
        public string SLA { get; set; }
        public decimal? SoftwareValue { get; set; }

        // Backup Officers
        public int? BackupOfficer1Id { get; set; }
        public Employee? BackupOfficer1 { get; set; }

        public int? BackupOfficer2Id { get; set; }
        public Employee? BackupOfficer2 { get; set; }

        public DateTime? SSLCertificateExpDate { get; set; }

        // DPO fields
        public DateTime? DPOHandoverDate { get; set; }
        public string DPOHandoverComment { get; set; }

        // Added properties to hold JOINed data from the data access layer
        public string? DevelopedByName { get; set; }
        public string? CompanyName { get; set; }
        public string? SalesTeamName { get; set; }
        public string? SdlcPhaseName { get; set; }
    }
}
