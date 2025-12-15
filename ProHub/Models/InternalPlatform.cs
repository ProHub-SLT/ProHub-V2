// Models/InternalPlatform.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace ProHub.Models
{
    public class InternalPlatform
    {
        public int Id { get; set; }

        public string? AppName { get; set; }
        public int? DevelopedById { get; set; }
        public Employee? DevelopedBy { get; set; }
        public string? DevelopedTeam { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? BitBucket { get; set; }
        public string? BitBucketRepo { get; set; }
        public string PlatformType { get; set; }
        public string PlatformName { get; set; }

        public int? SDLCPhaseId { get; set; }
        public SDLCPhase? SDLCPhase { get; set; }

        public decimal? PercentageDone { get; set; }
        public string? Status { get; set; }
        public DateTime? StatusDate { get; set; }
        public string? BusOwner { get; set; }
        public string? AppCategory { get; set; }
        public string? Scope { get; set; }
        public string? AppIP { get; set; }
        public string? AppURL { get; set; }
        public string? AppUsers { get; set; }
        public DateTime? UATDate { get; set; }
        public string? IntegratedApps { get; set; }
        public string? DR { get; set; }
        public DateTime? LaunchedDate { get; set; }
        public DateTime? VADate { get; set; }
        public string? WAF { get; set; }
        public string? APPOwner { get; set; }
        public string? AppBusinessOwner { get; set; }
        public decimal? Price { get; set; }
        public int? EndUserTypeId { get; set; }
        public TargetEndUser? EndUserType { get; set; }
        public string? RequestNo { get; set; }
        public int? ParentProjectID { get; set; }
        public ParentProject? ParentProject { get; set; }
        public string? SLA { get; set; }
        public int? BackupOfficer1Id { get; set; }
        public Employee? BackupOfficer1 { get; set; }
        public int? BackupOfficer2Id { get; set; }
        public Employee? BackupOfficer2 { get; set; }
        public int? MainAppID { get; set; }
        public InternalPlatform? MainApp { get; set; }
        public DateTime? SSLCertificateExpDate { get; set; }
        public DateTime? DPOHandoverDate { get; set; }
        public string? DPOHandoverComment { get; set; }
        public decimal? IncentiveEarned { get; set; }
        public DateTime? BillingDate { get; set; }
        public string ContractPeriod { get; set; }

        public int? SalesTeamId { get; set; }
        public SalesTeam SalesTeam { get; set; }


        public int? CompanyId { get; set; }
        public Company Company { get; set; }

        public string ProposalUploaded { get; set; }
        public string PlatformOwner { get; set; }
        public string APP_Owner { get; set; }
        public decimal? PlatformOTC { get; set; }
        public decimal? PlatformMRC { get; set; }
        public decimal? IncentiveShare { get; set; }
        public decimal? SoftwareValue { get; set; }




        // Navigation property for project comments
        public List<InternalProjectComment>? ProjectComments { get; set; }

        // --- Properties for JOINed data (for display) ---
        public string? DevelopedByName { get; set; }
        public string? SDLCPhaseName { get; set; }
        public string? ParentProjectName { get; set; }
        public string? EndUserTypeName { get; set; }
        public string? BackupOfficer1Name { get; set; }
        public string? BackupOfficer2Name { get; set; }
        public string? MainAppName { get; set; }

        public string? ParentProjectGroupName { get; set; }

        public string? Comment { get; set; }


        [NotMapped]
        public int CommentCount { get; set; }
    }
}