using MySql.Data.MySqlClient;
using ProHub.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace ProHub.Data
{
    public class ExternalSolutionRepository
    {
        private readonly IConfiguration _configuration;

        public ExternalSolutionRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
            => new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        private T GetValueOrDefault<T>(MySqlDataReader reader, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return defaultValue;
                return (T)reader.GetValue(ordinal);
            }
            catch
            {
                return defaultValue;
            }
        }

        // Get abandoned solutions list (table view)
        public List<ExternalPlatform> GetAbandonedSolutions(string search = "")
        {
            var list = new List<ExternalPlatform>();
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    ep.ID AS Id,
                    ep.Platform_Name AS PlatformName,
                    e1.Emp_ID AS DevelopedById,
                    e1.Emp_Name AS DevelopedByName,
                    sp.ID AS SDLCStageId,
                    sp.Phase AS SDLCPhaseName,
                    ep.StartDate AS StartDate,
                    ep.DPO_Handover_Date AS DPOHandoverDate,
                    ep.DPO_Handover_Comment AS DPOHandoverComment,
                    c.ID AS CompanyId,
                    c.Company_Name AS CompanyName
                FROM external_platforms ep
                INNER JOIN company c ON ep.Company_ID = c.ID
                LEFT JOIN Employee e1 ON ep.Developed_By = e1.Emp_ID
                LEFT JOIN SDLCPhas sp ON ep.SDLCstage = sp.ID
                WHERE sp.Phase = 'Abandoned'";

            if (!string.IsNullOrEmpty(search))
            {
                query += @" AND (
                    ep.Platform_Name LIKE @search OR
                    c.Company_Name LIKE @search OR
                    e1.Emp_Name LIKE @search
                )";
            }

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("@search", $"%{search}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ExternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    PlatformName = GetValueOrDefault(reader, "PlatformName", ""),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? (DateTime?)null : reader.GetDateTime("StartDate"),
                    DPOHandoverDate = reader.IsDBNull(reader.GetOrdinal("DPOHandoverDate")) ? (DateTime?)null : reader.GetDateTime("DPOHandoverDate"),
                    DPOHandoverComment = GetValueOrDefault(reader, "DPOHandoverComment", ""),

                    DevelopedBy = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "DevelopedById", 0),
                        EmpName = GetValueOrDefault(reader, "DevelopedByName", "")
                    },

                    SDLCStage = new SDLCPhase
                    {
                        Id = GetValueOrDefault(reader, "SDLCStageId", 0),
                        Phase = GetValueOrDefault(reader, "SDLCPhaseName", "")
                    },

                    Company = new Company
                    {
                        Id = GetValueOrDefault(reader, "CompanyId", 0),
                        CompanyName = GetValueOrDefault(reader, "CompanyName", "")
                    }
                });
            }

            return list;
        }

        // Get single abandoned solution by ID
        public ExternalPlatform GetAbandonedSolutionById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    ep.ID AS Id,
                    ep.Platform_Name AS PlatformName,
                    ep.Platform_Type AS PlatformType,
                    ep.StartDate AS StartDate,
                    ep.TargetDate AS TargetDate,
                    ep.PercentageDone AS PercentageDone,
                    ep.Status AS Status,
                    ep.StatusDate AS StatusDate,
                    ep.Integrated_apps AS IntegratedApps,
                    ep.DR AS DR,
                    ep.UATDate AS UATDate,
                    ep.VADate AS VADate,
                    ep.LaunchedDate AS LaunchedDate,
                    ep.Platform_Owner AS PlatformOwner,
                    ep.APP_OP_Owner AS APP_Owner,
                    ep.Platform_OTC AS PlatformOTC,
                    ep.Platform_MRC AS PlatformMRC,
                    ep.Contract_Period AS ContractPeriod,
                    ep.Incentive_Earned AS IncentiveEarned,
                    ep.Incentive_Share AS IncentiveShare,
                    ep.BillingDate AS BillingDate,
                    ep.Proposal_Upload AS ProposalUploaded,
                    ep.SLA AS SLA,
                    ep.Software_Value AS SoftwareValue,
                    ep.SSLCertificateExpDate AS SSLCertificateExpDate,
                    ep.DPO_Handover_Date AS DPOHandoverDate,
                    ep.DPO_Handover_Comment AS DPOHandoverComment,

                    -- Company
                    c.ID AS CompanyId,
                    c.Company_Name AS CompanyName,

                    -- DevelopedBy Employee
                    e1.Emp_ID AS DevelopedById,
                    e1.Emp_Name AS DevelopedByName,
                    e1.Emp_Email AS DevelopedByEmail,
                    e1.Emp_Phone AS DevelopedByPhone,

                    -- BackupOfficer1
                    e2.Emp_ID AS Backup1Id,
                    e2.Emp_Name AS Backup1Name,
                    e2.Emp_Email AS Backup1Email,

                    -- BackupOfficer2
                    e3.Emp_ID AS Backup2Id,
                    e3.Emp_Name AS Backup2Name,
                    e3.Emp_Email AS Backup2Email,

                    -- SalesTeam
                    st.ID AS SalesTeamId,
                    st.Sales_Team_Name AS SalesTeamName,
                    st.Sales_Team_Group AS SalesTeamGroup,
                    st.Sales_Team_Head AS SalesTeamHead,

                    -- SDLCPhase
                    sp.ID AS SDLCStageId,
                    sp.Phase AS SDLCPhaseName,
                    sp.OrderSeq AS SDLCOrderSeq

                FROM external_platforms ep
                INNER JOIN company c ON ep.Company_ID = c.ID
                LEFT JOIN Employee e1 ON ep.Developed_By = e1.Emp_ID
                LEFT JOIN Employee e2 ON ep.BackupOfficer_1 = e2.Emp_ID
                LEFT JOIN Employee e3 ON ep.BackupOfficer_2 = e3.Emp_ID
                LEFT JOIN Sales_Team st ON ep.Sales_Team_ID = st.ID
                LEFT JOIN SDLCPhas sp ON ep.SDLCstage = sp.ID
                WHERE ep.ID = @id AND sp.Phase = 'Abandoned'";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new ExternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    PlatformName = GetValueOrDefault(reader, "PlatformName", ""),
                    PlatformType = GetValueOrDefault(reader, "PlatformType", ""),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? (DateTime?)null : reader.GetDateTime("StartDate"),
                    TargetDate = reader.IsDBNull(reader.GetOrdinal("TargetDate")) ? (DateTime?)null : reader.GetDateTime("TargetDate"),
                    PercentageDone = GetValueOrDefault(reader, "PercentageDone", 0m),
                    Status = GetValueOrDefault(reader, "Status", ""),
                    StatusDate = reader.IsDBNull(reader.GetOrdinal("StatusDate")) ? (DateTime?)null : reader.GetDateTime("StatusDate"),
                    IntegratedApps = GetValueOrDefault(reader, "IntegratedApps", ""),
                    DR = GetValueOrDefault(reader, "DR", ""),
                    UATDate = reader.IsDBNull(reader.GetOrdinal("UATDate")) ? (DateTime?)null : reader.GetDateTime("UATDate"),
                    VADate = reader.IsDBNull(reader.GetOrdinal("VADate")) ? (DateTime?)null : reader.GetDateTime("VADate"),
                    LaunchedDate = reader.IsDBNull(reader.GetOrdinal("LaunchedDate")) ? (DateTime?)null : reader.GetDateTime("LaunchedDate"),
                    PlatformOwner = GetValueOrDefault(reader, "PlatformOwner", ""),
                    APP_Owner = GetValueOrDefault(reader, "APP_Owner", ""),
                    PlatformOTC = GetValueOrDefault(reader, "PlatformOTC", 0m),
                    PlatformMRC = GetValueOrDefault(reader, "PlatformMRC", 0m),
                    ContractPeriod = GetValueOrDefault(reader, "ContractPeriod", ""),
                    IncentiveEarned = GetValueOrDefault(reader, "IncentiveEarned", 0m),
                    IncentiveShare = GetValueOrDefault(reader, "IncentiveShare", 0m),
                    BillingDate = reader.IsDBNull(reader.GetOrdinal("BillingDate")) ? (DateTime?)null : reader.GetDateTime("BillingDate"),
                    ProposalUploaded = GetValueOrDefault(reader, "ProposalUploaded", ""),
                    SLA = GetValueOrDefault(reader, "SLA", ""),
                    SoftwareValue = GetValueOrDefault(reader, "SoftwareValue", 0m),
                    SSLCertificateExpDate = reader.IsDBNull(reader.GetOrdinal("SSLCertificateExpDate")) ? (DateTime?)null : reader.GetDateTime("SSLCertificateExpDate"),
                    DPOHandoverDate = reader.IsDBNull(reader.GetOrdinal("DPOHandoverDate")) ? (DateTime?)null : reader.GetDateTime("DPOHandoverDate"),
                    DPOHandoverComment = GetValueOrDefault(reader, "DPOHandoverComment", ""),

                    Company = new Company
                    {
                        Id = GetValueOrDefault(reader, "CompanyId", 0),
                        CompanyName = GetValueOrDefault(reader, "CompanyName", "")
                    },

                    DevelopedBy = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "DevelopedById", 0),
                        EmpName = GetValueOrDefault(reader, "DevelopedByName", ""),
                        EmpEmail = GetValueOrDefault(reader, "DevelopedByEmail", ""),
                        EmpPhone = GetValueOrDefault(reader, "DevelopedByPhone", "")
                    },

                    BackupOfficer1 = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup1Id", 0),
                        EmpName = GetValueOrDefault(reader, "Backup1Name", ""),
                        EmpEmail = GetValueOrDefault(reader, "Backup1Email", "")
                    },

                    BackupOfficer2 = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup2Id", 0),
                        EmpName = GetValueOrDefault(reader, "Backup2Name", ""),
                        EmpEmail = GetValueOrDefault(reader, "Backup2Email", "")
                    },

                    SalesTeam = new SalesTeam
                    {
                        Id = GetValueOrDefault(reader, "SalesTeamId", 0),
                        SalesTeamName = GetValueOrDefault(reader, "SalesTeamName", ""),
                        SalesTeamGroup = GetValueOrDefault(reader, "SalesTeamGroup", ""),
                        SalesTeamHead = GetValueOrDefault(reader, "SalesTeamHead", "")
                    },

                    SDLCStage = new SDLCPhase
                    {
                        Id = GetValueOrDefault(reader, "SDLCStageId", 0),
                        Phase = GetValueOrDefault(reader, "SDLCPhaseName", ""),
                        OrderSeq = GetValueOrDefault(reader, "SDLCOrderSeq", 0)
                    }
                };
            }

            return null;
        }

        // Get all external_platform names 
        public List<ExternalPlatform> GetAll()
        {
            var list = new List<ExternalPlatform>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID, Platform_Name FROM External_Platforms ORDER BY Platform_Name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ExternalPlatform { Id = r.GetInt32(0), PlatformName = r.GetString(1) });
            return list;
        }

        public List<ExternalPlatform> GetByIds(List<int> ids)
        {
            if (!ids.Any()) return new();
            using var conn = GetConnection();
            conn.Open();
            var placeholders = string.Join(",", ids.Select((_, i) => $"@p{i}"));
            var sql = $"SELECT ID, Platform_Name FROM External_Platforms WHERE ID IN ({placeholders})";
            using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < ids.Count; i++)
                cmd.Parameters.AddWithValue($"@p{i}", ids[i]);
            var list = new List<ExternalPlatform>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ExternalPlatform { Id = r.GetInt32(0), PlatformName = r.GetString(1) });
            return list;
        }

        // New method to get all external platforms with backup information
        public List<ExternalPlatform> GetAllExternalPlatformsWithBackupInfo()
        {
            var list = new List<ExternalPlatform>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    ep.ID AS Id,
                    ep.Platform_Name AS PlatformName,
                    ep.BackupOfficer_1 AS BackupOfficer1Id,
                    e1.Emp_ID AS Backup1EmpId,
                    e1.Emp_Name AS Backup1Name,
                    ep.BackupOfficer_2 AS BackupOfficer2Id,
                    e2.Emp_ID AS Backup2EmpId,
                    e2.Emp_Name AS Backup2Name
                FROM external_platforms ep
                LEFT JOIN Employee e1 ON ep.BackupOfficer_1 = e1.Emp_ID
                LEFT JOIN Employee e2 ON ep.BackupOfficer_2 = e2.Emp_ID
                ORDER BY ep.Platform_Name";

            using var cmd = new MySqlCommand(query, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var platform = new ExternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    PlatformName = GetValueOrDefault(reader, "PlatformName", ""),
                    BackupOfficer1Id = GetValueOrDefault(reader, "BackupOfficer1Id", (int?)null),
                    BackupOfficer1 = reader.IsDBNull(reader.GetOrdinal("Backup1EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup1EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup1Name", "")
                    },
                    BackupOfficer2Id = GetValueOrDefault(reader, "BackupOfficer2Id", (int?)null),
                    BackupOfficer2 = reader.IsDBNull(reader.GetOrdinal("Backup2EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup2EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup2Name", "")
                    }
                };

                list.Add(platform);
            }

            return list;
        }

        // Get all main platforms
        public List<MainPlatform> GetAllMainPlatforms()
        {
            var list = new List<MainPlatform>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID, Platforms FROM Main_Platforms ORDER BY Platforms", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MainPlatform { ID = r.GetInt32(0), Platforms = r.IsDBNull(1) ? null : r.GetString(1) });
            return list;
        }

        // Get the ID of the "external" platform from Main_Platforms table
        public int? GetExternalPlatformId()
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID FROM Main_Platforms WHERE LOWER(Platforms) = 'external' LIMIT 1", conn);
            using var r = cmd.ExecuteReader();
            if (r.Read())
                return r.GetInt32(0);
            return null;
        }
    }
}
