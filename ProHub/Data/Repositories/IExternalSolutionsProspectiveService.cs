using MySql.Data.MySqlClient;
using ProHub.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PROHUB.Data
{
    public interface IExternalSolutionsProspectiveService
    {
        // Read & List Methods
        Task<List<ExternalPlatform>> GetProspectiveSolutionsAsync(string search);
        Task<List<ExternalPlatform>> GetInProgressSolutionsAsync(string search);
        Task<ExternalPlatform?> GetByIdAsync(int id);
        Task<DataTable> GetProspectiveExportDataAsync();

        // CRUD Operations
        Task<int> CreateAsync(ExternalPlatform externalSolution);
        Task<bool> UpdateAsync(ExternalPlatform externalSolution);
        Task<bool> DeleteAsync(int id);

        // Dropdowns Helpers
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<Company>> GetCompanyAsync();
        Task<List<SalesTeam>> Getsales_teamAsync();
        Task<List<SDLCPhase>> GetsdlcphasAsync();
    }


    public class ExternalSolutionsProspectiveDataAccess : IExternalSolutionsProspectiveService
    {
        private readonly string _connectionString;

        public ExternalSolutionsProspectiveDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        
        //  READ LISTS
        
        public async Task<List<ExternalPlatform>> GetProspectiveSolutionsAsync(string search)
        {
            return await GetSolutionsAsync(search, "Prospective");
        }

        public async Task<List<ExternalPlatform>> GetInProgressSolutionsAsync(string search)
        {
            return await GetSolutionsAsync(search, "InProgress");
        }

        private async Task<List<ExternalPlatform>> GetSolutionsAsync(string search, string type)
        {
            var list = new List<ExternalPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            string filterClause = type == "Prospective"
                 ? "LOWER(TRIM(sp.Phase)) LIKE 'prospective%'"
                : "(LOWER(TRIM(sp.Phase)) NOT LIKE '%maintenance%' AND LOWER(TRIM(sp.Phase)) NOT LIKE '%retired%' AND LOWER(TRIM(sp.Phase)) NOT LIKE '%abandoned%' AND LOWER(TRIM(sp.Phase)) NOT LIKE '%prospective%')";

            string query = $@"
                SELECT 
                    ep.ID, ep.Platform_Name, ep.StartDate, ep.DPO_Handover_Date, ep.DPO_Handover_Comment, ep.Sales_AM, 
                    ep.Platform_OTC, ep.Platform_MRC,
                    e1.Emp_ID AS DevelopedById, e1.Emp_Name AS DevelopedByName,
                    sp.ID AS SDLCStageId, sp.Phase AS SDLCPhaseName,
                    c.ID AS CompanyId, c.Company_Name AS CompanyName
                FROM external_platforms ep
                LEFT JOIN Employee e1 ON ep.Developed_By = e1.Emp_ID
                LEFT JOIN SDLCPhas sp ON ep.SDLCStage = sp.ID
                LEFT JOIN Company c ON ep.Company_ID = c.ID
                WHERE {filterClause}";

            if (!string.IsNullOrWhiteSpace(search))
            {
                query += " AND (ep.Platform_Name LIKE @search OR e1.Emp_Name LIKE @search OR c.Company_Name LIKE @search)";
            }

            query += " ORDER BY ep.Platform_Name";

            using var command = new MySqlCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(search))
            {
                command.Parameters.AddWithValue("@search", $"%{search.Trim()}%");
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToExternalPlatform(reader)); // Note: Using simplified mapper for list
            }
            return list;
        }

      
        //  GET BY ID
        
        public async Task<ExternalPlatform?> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            // Fetching full details for Edit/Details view
            string query = @"
                SELECT 
                    ep.*,
                    e1.Emp_ID AS DevelopedById, e1.Emp_Name AS DevelopedByName,
                    sp.ID AS SDLCStageId, sp.Phase AS SDLCPhaseName,
                    c.ID AS CompanyId, c.Company_Name AS CompanyName,
                    e2.Emp_ID AS BackupOfficer1Id, e2.Emp_Name AS BackupOfficer1Name,
                    e3.Emp_ID AS BackupOfficer2Id, e3.Emp_Name AS BackupOfficer2Name
                FROM external_platforms ep
                LEFT JOIN Employee e1 ON ep.Developed_By = e1.Emp_ID
                LEFT JOIN Employee e2 ON ep.BackupOfficer_1 = e2.Emp_ID
                LEFT JOIN Employee e3 ON ep.BackupOfficer_2 = e3.Emp_ID
                LEFT JOIN SDLCPhas sp ON ep.SDLCStage = sp.ID
                LEFT JOIN Company c ON ep.Company_ID = c.ID
                WHERE ep.ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToFullExternalPlatform(reader);
            }
            return null;
        }

        
        //  CREATE 
       
        public async Task<int> CreateAsync(ExternalPlatform model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO external_platforms (
                    Platform_Name, Company_ID, Developed_By, Developed_Team, StartDate, TargetDate,
                    SDLCstage, PercentageDone, BIT_bucket_repo, Sales_Team_ID, Sales_AM, Sales_Manager,
                    Sales_Enginneer, UATDate, LaunchedDate, Platform_OTC, Platform_MRC, Software_Value,
                    Contract_Period, SLA, DPO_Handover_Date, DPO_Handover_Comment,
                    SSLCertificateExpDate, BillingDate, Proposal_Upload, Incentive_Earned, Incentive_Share,
                    BackupOfficer_1, BackupOfficer_2
                ) VALUES (
                    @PlatformName, @CompanyId, @DevelopedBy, @DevelopedTeam, @StartDate, @TargetDate,
                    @SDLCStage, @PercentageDone, @BITBucketRepo, @SalesTeamId, @SalesAM, @SalesManager,
                    @SalesEngineer, @UATDate, @LaunchedDate, @PlatformOTC, @PlatformMRC, @SoftwareValue,
                    @ContractPeriod, @SLA, @DPOHandoverDate, @DPOHandoverComment,
                    @SSLCertificateExpDate, @BillingDate, @ProposalUploaded, @IncentiveEarned, @IncentiveShare,
                    @BackupOfficer1Id, @BackupOfficer2Id
                );
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, model);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        
        //  UPDATE
        
        public async Task<bool> UpdateAsync(ExternalPlatform model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Id <= 0) throw new ArgumentException("Invalid ID", nameof(model.Id));

            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                UPDATE external_platforms SET
                    Platform_Name = @PlatformName,
                    Company_ID = @CompanyId,
                    Developed_By = @DevelopedBy,
                    Developed_Team = @DevelopedTeam,
                    StartDate = @StartDate,
                    TargetDate = @TargetDate,
                    SDLCstage = @SDLCStage,
                    PercentageDone = @PercentageDone,
                    BIT_bucket_repo = @BITBucketRepo,
                    Sales_Team_ID = @SalesTeamId,
                    Sales_AM = @SalesAM,
                    Sales_Manager = @SalesManager,
                    Sales_Enginneer = @SalesEngineer,
                    UATDate = @UATDate,
                    LaunchedDate = @LaunchedDate,
                    Platform_OTC = @PlatformOTC,
                    Platform_MRC = @PlatformMRC,
                    Software_Value = @SoftwareValue,
                    Contract_Period = @ContractPeriod,
                    SLA = @SLA,
                    DPO_Handover_Date = @DPOHandoverDate,
                    DPO_Handover_Comment = @DPOHandoverComment,
                    SSLCertificateExpDate = @SSLCertificateExpDate,
                    BillingDate = @BillingDate,
                    Proposal_Upload = @ProposalUploaded,
                    Incentive_Earned = @IncentiveEarned,
                    Incentive_Share = @IncentiveShare,
                    BackupOfficer_1 = @BackupOfficer1Id,
                    BackupOfficer_2 = @BackupOfficer2Id
                WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", model.Id);
            AddParameters(command, model);

            return await command.ExecuteNonQueryAsync() > 0;
        }

      
        //  DELETE
        
        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) return false;

            using var connection = GetConnection();
            await connection.OpenAsync();

            string query = "DELETE FROM external_platforms WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        
        //  EXPORT DATA
        
        public async Task<DataTable> GetProspectiveExportDataAsync()
        {
            var dt = new DataTable("external_platforms");
            using var connection = GetConnection();
            await connection.OpenAsync();

            string query = @"
                SELECT ep.*, sp.Phase as SDLC_Phase, c.Company_Name
                FROM external_platforms ep
                LEFT JOIN SDLCPhas sp ON ep.SDLCStage = sp.ID
                LEFT JOIN Company c ON ep.Company_ID = c.ID
                WHERE (LOWER(TRIM(sp.Phase)) LIKE 'prospective%' OR LOWER(TRIM(ep.Status)) LIKE 'prospective%' OR ep.SDLCStage = 10)
                ORDER BY ep.Platform_Name";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        
        //  DROPDOWN HELPERS
       
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("SELECT Emp_ID, Emp_Name FROM Employee ORDER BY Emp_Name", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Employee { EmpId = GetInt32Safe(reader, "Emp_ID"), EmpName = GetNullableString(reader, "Emp_Name") ?? "" });
            }
            return list;
        }

        public async Task<List<Company>> GetCompanyAsync()
        {
            var list = new List<Company>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, Company_Name FROM Company WHERE Company_Name IS NOT NULL AND Company_Name != '' ORDER BY Company_Name", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Company { Id = GetInt32Safe(reader, "ID"), CompanyName = GetNullableString(reader, "Company_Name") ?? "" });
            }
            return list;
        }

        public async Task<List<SalesTeam>> Getsales_teamAsync()
        {
            var list = new List<SalesTeam>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, Sales_Team_Name FROM Sales_Team ORDER BY Sales_Team_Name", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SalesTeam { Id = GetInt32Safe(reader, "ID"), SalesTeamName = GetNullableString(reader, "Sales_Team_Name") ?? "" });
            }
            return list;
        }

        public async Task<List<SDLCPhase>> GetsdlcphasAsync()
        {
            var list = new List<SDLCPhase>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, Phase FROM SDLCPhas ORDER BY OrderSeq, Phase", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new SDLCPhase { Id = GetInt32Safe(reader, "ID"), Phase = GetNullableString(reader, "Phase") ?? "" });
            }
            return list;
        }

        
        //  MAPPERS & HELPERS
        

        private void AddParameters(MySqlCommand command, ExternalPlatform model)
        {
            command.Parameters.AddWithValue("@PlatformName", (object?)model.PlatformName ?? DBNull.Value);
            command.Parameters.AddWithValue("@CompanyId", (object?)model.CompanyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DevelopedBy", (object?)model.DevelopedById ?? DBNull.Value);
            command.Parameters.AddWithValue("@DevelopedTeam", (object?)model.DevelopedTeam ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object?)model.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@TargetDate", (object?)model.TargetDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@SDLCStage", (object?)model.SDLCStageId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PercentageDone", (object?)model.PercentageDone ?? DBNull.Value);
            command.Parameters.AddWithValue("@BITBucketRepo", (object?)model.BITBucketRepo ?? DBNull.Value);
            command.Parameters.AddWithValue("@SalesTeamId", (object?)model.SalesTeamId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SalesAM", (object?)model.SalesAM ?? DBNull.Value);
            command.Parameters.AddWithValue("@SalesManager", (object?)model.SalesManager ?? DBNull.Value);
            command.Parameters.AddWithValue("@SalesEngineer", (object?)model.SalesEngineer ?? DBNull.Value);
            command.Parameters.AddWithValue("@UATDate", (object?)model.UATDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@LaunchedDate", (object?)model.LaunchedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@PlatformOTC", (object?)model.PlatformOTC ?? DBNull.Value);
            command.Parameters.AddWithValue("@PlatformMRC", (object?)model.PlatformMRC ?? DBNull.Value);
            command.Parameters.AddWithValue("@SoftwareValue", (object?)model.SoftwareValue ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContractPeriod", (object?)model.ContractPeriod ?? DBNull.Value);
            command.Parameters.AddWithValue("@SLA", (object?)model.SLA ?? DBNull.Value);
            command.Parameters.AddWithValue("@DPOHandoverDate", (object?)model.DPOHandoverDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DPOHandoverComment", (object?)model.DPOHandoverComment ?? DBNull.Value);
            command.Parameters.AddWithValue("@SSLCertificateExpDate", (object?)model.SSLCertificateExpDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@BillingDate", (object?)model.BillingDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProposalUploaded", (object?)model.ProposalUploaded ?? DBNull.Value);
            command.Parameters.AddWithValue("@IncentiveEarned", (object?)model.IncentiveEarned ?? DBNull.Value);
            command.Parameters.AddWithValue("@IncentiveShare", (object?)model.IncentiveShare ?? DBNull.Value);
            command.Parameters.AddWithValue("@BackupOfficer1Id", (object?)model.BackupOfficer1Id ?? DBNull.Value);
            command.Parameters.AddWithValue("@BackupOfficer2Id", (object?)model.BackupOfficer2Id ?? DBNull.Value);
        }

        // Simplified mapper for Index lists (fewer fields needed)
        private ExternalPlatform MapReaderToExternalPlatform(IDataReader reader)
        {
            return new ExternalPlatform
            {
                Id = GetInt32Safe(reader, "ID"),
                PlatformName = GetNullableString(reader, "Platform_Name") ?? string.Empty,
                StartDate = GetNullableDateTime(reader, "StartDate"),
                DPOHandoverDate = GetNullableDateTime(reader, "DPO_Handover_Date"),
                DPOHandoverComment = GetNullableString(reader, "DPO_Handover_Comment"),
                SalesAM = GetNullableString(reader, "Sales_AM"),
                PlatformOTC = GetNullableDecimal(reader, "Platform_OTC"),
                PlatformMRC = GetNullableDecimal(reader, "Platform_MRC"),
                DevelopedBy = new Employee
                {
                    EmpId = GetInt32Safe(reader, "DevelopedById"),
                    EmpName = GetNullableString(reader, "DevelopedByName") ?? string.Empty
                },
                SDLCStage = new SDLCPhase
                {
                    Id = GetInt32Safe(reader, "SDLCStageId"),
                    Phase = GetNullableString(reader, "SDLCPhaseName") ?? string.Empty
                },
                Company = new Company
                {
                    Id = GetInt32Safe(reader, "CompanyId"),
                    CompanyName = GetNullableString(reader, "CompanyName") ?? string.Empty
                }
            };
        }

        // Full mapper for Edit/Details (includes all fields)
        private ExternalPlatform MapReaderToFullExternalPlatform(IDataReader reader)
        {
            // Reuse logic or map manually
            var model = MapReaderToExternalPlatform(reader);

            // Map extra fields not in the list view
            model.CompanyId = GetNullableInt32(reader, "Company_ID");
            model.DevelopedById = GetNullableInt32(reader, "Developed_By");
            model.SDLCStageId = GetNullableInt32(reader, "SDLCstage");
            model.SalesTeamId = GetNullableInt32(reader, "Sales_Team_ID");
            model.BackupOfficer1Id = GetNullableInt32(reader, "BackupOfficer1Id");
            model.BackupOfficer2Id = GetNullableInt32(reader, "BackupOfficer2Id");

            model.DevelopedTeam = GetNullableString(reader, "Developed_Team");
            model.TargetDate = GetNullableDateTime(reader, "TargetDate");
            model.PercentageDone = GetNullableDecimal(reader, "PercentageDone");
            model.BITBucketRepo = GetNullableString(reader, "BIT_bucket_repo");
            model.SalesManager = GetNullableString(reader, "Sales_Manager");
            model.SalesEngineer = GetNullableString(reader, "Sales_Enginneer");
            model.UATDate = GetNullableDateTime(reader, "UATDate");
            model.LaunchedDate = GetNullableDateTime(reader, "LaunchedDate");
            model.SoftwareValue = GetNullableDecimal(reader, "Software_Value");
            model.ContractPeriod = GetNullableString(reader, "Contract_Period");
            model.SLA = GetNullableString(reader, "SLA");
            model.DPOHandoverComment = GetNullableString(reader, "DPO_Handover_Comment");
            model.SSLCertificateExpDate = GetNullableDateTime(reader, "SSLCertificateExpDate");
            model.BillingDate = GetNullableDateTime(reader, "BillingDate");
            model.ProposalUploaded = GetNullableString(reader, "Proposal_Upload");
            model.IncentiveEarned = GetNullableDecimal(reader, "Incentive_Earned");
            model.IncentiveShare = GetNullableDecimal(reader, "Incentive_Share");

            // Map backup officers
            if (model.BackupOfficer1Id.HasValue)
            {
                model.BackupOfficer1 = new Employee
                {
                    EmpId = model.BackupOfficer1Id.Value,
                    EmpName = GetNullableString(reader, "BackupOfficer1Name") ?? string.Empty
                };
            }

            if (model.BackupOfficer2Id.HasValue)
            {
                model.BackupOfficer2 = new Employee
                {
                    EmpId = model.BackupOfficer2Id.Value,
                    EmpName = GetNullableString(reader, "BackupOfficer2Name") ?? string.Empty
                };
            }

            return model;
        }

        private static int GetInt32Safe(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt32(ord);
            }
            catch { return 0; }
        }

        private static int? GetNullableInt32(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetInt32(ord);
            }
            catch { return null; }
        }

        private static string? GetNullableString(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ord)) return null;
                return reader.GetValue(ord).ToString();
            }
            catch { return null; }
        }

        private static DateTime? GetNullableDateTime(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetDateTime(ord);
            }
            catch { return null; }
        }

        private static decimal? GetNullableDecimal(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? null : reader.GetDecimal(ord);
            }
            catch { return null; }
        }
    }
}
