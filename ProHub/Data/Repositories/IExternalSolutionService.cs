using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;

namespace PROHUB.Data
{
    public interface IExternalSolutionService
    {
        Task<List<ExternalPlatform>> GetAllAsync();
        Task<ExternalPlatform?> GetByIdAsync(int id);
        Task<int> CreateAsync(ExternalPlatform externalSolution);
        Task<bool> UpdateAsync(ExternalPlatform externalSolution);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<Company>> GetCompanyAsync();
        Task<List<SalesTeam>> Getsales_teamAsync();
        Task<List<SDLCPhase>> GetsdlcphasAsync();
    }

    public class ExternalSolutionDataAccess : IExternalSolutionService
    {
        private readonly string _connectionString;

        public ExternalSolutionDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string not found");
        }

        public async Task<List<ExternalPlatform>> GetAllAsync()
        {
            var list = new List<ExternalPlatform>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT 
                    ex.*,
                    emp.Emp_Name AS DevelopedByName,
                    c.Company_Name AS CompanyName,
                    st.Sales_Team_Name AS SalesTeamName,
                    sdlc.Phase AS SdlcPhaseName
                FROM external_platforms ex
                LEFT JOIN Employee emp ON ex.Developed_By = emp.Emp_ID     
                LEFT JOIN Company c ON ex.Company_ID = c.ID            
                LEFT JOIN Sales_Team st ON ex.Sales_Team_ID = st.ID   
                LEFT JOIN SDLCPhas sdlc ON ex.SDLCstage = sdlc.ID        
                ORDER BY ex.ID DESC";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToExternalSolution(reader));
            }

            return list;
        }

        public async Task<ExternalPlatform?> GetByIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT 
                    ex.*,
                    emp.Emp_Name AS DevelopedByName,
                    c.Company_Name AS CompanyName,
                    st.Sales_Team_Name AS SalesTeamName,
                    sdlc.Phase AS SdlcPhaseName
                FROM external_platforms ex
                LEFT JOIN Employee emp ON ex.Developed_By = emp.Emp_ID    
                LEFT JOIN Company c ON ex.Company_ID = c.ID            
                LEFT JOIN Sales_Team st ON ex.Sales_Team_ID = st.ID    
                LEFT JOIN SDLCPhas sdlc ON ex.SDLCstage = sdlc.ID        
                WHERE ex.ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;

            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReaderToExternalSolution(reader) : null;
        }

        public async Task<int> CreateAsync(ExternalPlatform model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO external_platforms (
                    Platform_Name, Company_ID, Developed_By, Developed_Team, StartDate, TargetDate,
                    SDLCstage, PercentageDone, BIT_bucket_repo, Sales_Team_ID, Sales_AM, Sales_Manager,
                    Sales_Enginneer, UATDate, LaunchedDate, Platform_OTC, Platform_MRC, Software_Value,
                    Contract_Period, SLA, DPO_Handover_Date, DPO_Handover_Comment,
                    SSLCertificateExpDate, BillingDate, Proposal_Upload
                ) VALUES (
                    @PlatformName, @CompanyId, @DevelopedBy, @DevelopedTeam, @StartDate, @TargetDate,
                    @SDLCStage, @PercentageDone, @BITBucketRepo, @SalesTeamId, @SalesAM, @SalesManager,
                    @SalesEngineer, @UATDate, @LaunchedDate, @PlatformOTC, @PlatformMRC, @SoftwareValue,
                    @ContractPeriod, @SLA, @DPOHandoverDate, @DPOHandoverComment,
                    @SSLCertificateExpDate, @BillingDate, @ProposalUploaded
                );
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, model);

            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public async Task<bool> UpdateAsync(ExternalPlatform model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (model.Id <= 0) throw new ArgumentException("Invalid ID", nameof(model.Id));

            using var connection = new MySqlConnection(_connectionString);
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
                    VADate = @VADate,
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
                    Proposal_Uploaded = @ProposalUploaded,
                    IncentiveEarned = @IncentiveEarned,
                    IncentiveShare = @IncentiveShare
                WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add("@Id", MySqlDbType.Int32).Value = model.Id;
            AddParameters(command, model);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0) throw new ArgumentException("Invalid ID", nameof(id));

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM external_platforms WHERE Id = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            if (id <= 0) return false;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT COUNT(1) FROM external_platforms WHERE Id = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.Add("@Id", MySqlDbType.Int32).Value = id;

            var result = await command.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) > 0;
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT Emp_ID, Emp_Name FROM Employee ORDER BY Emp_Name";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToEmployee(reader));
            }
            return list;
        }

        public async Task<List<Company>> GetCompanyAsync()
        {
            var list = new List<Company>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT ID, Company_Name FROM Company WHERE Company_Name IS NOT NULL AND Company_Name != '' ORDER BY Company_Name";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToCompany(reader));
            }
            return list;
        }

        public async Task<List<SalesTeam>> Getsales_teamAsync()
        {
            var list = new List<SalesTeam>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT ID, Sales_Team_Name FROM Sales_Team ORDER BY Sales_Team_Name";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToSalesTeam(reader));
            }
            return list;
        }

        public async Task<List<SDLCPhase>> GetsdlcphasAsync()
        {
            var list = new List<SDLCPhase>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT ID, Phase FROM SDLCPhas ORDER BY OrderSeq, Phase";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToSdlcphas(reader));
            }
            return list;
        }

        #region Mappers & Helpers

        private ExternalPlatform MapReaderToExternalSolution(IDataReader reader)
        {
            return new ExternalPlatform
            {
                Id = GetInt32Safe(reader, "ID"),
                PlatformName = GetNullableString(reader, "Platform_Name") ?? string.Empty,
                CompanyId = GetNullableInt32(reader, "Company_ID"),
                DevelopedById = GetNullableInt32(reader, "Developed_By"),
                DevelopedTeam = GetNullableString(reader, "Developed_Team"),
                StartDate = GetNullableDateTime(reader, "StartDate"),
                TargetDate = GetNullableDateTime(reader, "TargetDate"),
                SDLCStageId = GetNullableInt32(reader, "SDLCstage"),
                PercentageDone = GetNullableDecimal(reader, "PercentageDone"),
                BitBucket = GetNullableString(reader, "BitBucket") ?? GetNullableString(reader, "Bit_Bucket") ?? string.Empty,
                BITBucketRepo = GetNullableString(reader, "BIT_bucket_repo") ?? GetNullableString(reader, "BITBucketRepo") ?? string.Empty,
                SalesTeamId = GetNullableInt32(reader, "Sales_Team_ID"),
                SalesAM = GetNullableString(reader, "Sales_AM"),
                SalesManager = GetNullableString(reader, "Sales_Manager"),
                SalesEngineer = GetNullableString(reader, "Sales_Enginneer"),
                UATDate = GetNullableDateTime(reader, "UATDate"),
                VADate = GetNullableDateTime(reader, "VADate"),
                LaunchedDate = GetNullableDateTime(reader, "LaunchedDate"),
                PlatformOwner = GetNullableString(reader, "PlatformOwner"),
                APP_Owner = GetNullableString(reader, "APP_Owner"),
                PlatformOTC = GetNullableDecimal(reader, "Platform_OTC"),
                PlatformMRC = GetNullableDecimal(reader, "Platform_MRC"),
                ContractPeriod = GetNullableString(reader, "Contract_Period"),
                IncentiveEarned = GetNullableDecimal(reader, "IncentiveEarned"),
                IncentiveShare = GetNullableDecimal(reader, "IncentiveShare"),
                BillingDate = GetNullableDateTime(reader, "BillingDate"),
                ProposalUploaded = GetNullableString(reader, "Proposal_Uploaded"),
                SLA = GetNullableString(reader, "SLA"),
                SoftwareValue = GetNullableDecimal(reader, "Software_Value"),
                SSLCertificateExpDate = GetNullableDateTime(reader, "SSLCertificateExpDate"),
                DPOHandoverDate = GetNullableDateTime(reader, "DPO_Handover_Date"),
                DPOHandoverComment = GetNullableString(reader, "DPO_Handover_Comment"),

                // Joined display properties
                DevelopedByName = GetNullableString(reader, "DevelopedByName"),
                CompanyName = GetNullableString(reader, "CompanyName"),
                SalesTeamName = GetNullableString(reader, "SalesTeamName"),
                SdlcPhaseName = GetNullableString(reader, "SdlcPhaseName")
            };
        }

        private Employee MapReaderToEmployee(IDataReader reader)
        {
            return new Employee
            {
                EmpId = GetInt32Safe(reader, "Emp_ID"),
                EmpName = GetNullableString(reader, "Emp_Name") ?? string.Empty
            };
        }

        private Company MapReaderToCompany(IDataReader reader)
        {
            return new Company
            {
                Id = GetInt32Safe(reader, "ID"),
                CompanyName = GetNullableString(reader, "Company_Name") ?? string.Empty
            };
        }

        private SalesTeam MapReaderToSalesTeam(IDataReader reader)
        {
            return new SalesTeam
            {
                Id = GetInt32Safe(reader, "ID"),
                SalesTeamName = GetNullableString(reader, "Sales_Team_Name") ?? string.Empty
            };
        }

        private SDLCPhase MapReaderToSdlcphas(IDataReader reader)
        {
            return new SDLCPhase
            {
                Id = GetInt32Safe(reader, "ID"),
                Phase = GetNullableString(reader, "Phase") ?? string.Empty
            };
        }

        private static int GetInt32Safe(IDataReader reader, string columnName)
        {
            try
            {
                var ord = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ord) ? 0 : reader.GetInt32(ord);
            }
            catch (IndexOutOfRangeException)
            {
                return 0;
            }
        }

        private static int? GetNullableInt32(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private static decimal? GetNullableDecimal(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private static string? GetNullableString(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return reader.IsDBNull(i) ? null : reader.GetString(i);
                    }
                }
                return null;
            }
        }

        private static DateTime? GetNullableDateTime(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private void AddParameters(MySqlCommand command, ExternalPlatform model)
        {
            command.Parameters.Add("@PlatformName", MySqlDbType.VarChar).Value = (object?)model.PlatformName ?? DBNull.Value;
            command.Parameters.Add("@CompanyId", MySqlDbType.Int32).Value = (object?)model.CompanyId ?? DBNull.Value;
            command.Parameters.Add("@DevelopedBy", MySqlDbType.Int32).Value = (object?)model.DevelopedById ?? DBNull.Value;
            command.Parameters.Add("@DevelopedTeam", MySqlDbType.VarChar).Value = (object?)model.DevelopedTeam ?? DBNull.Value;
            command.Parameters.Add("@StartDate", MySqlDbType.DateTime).Value = (object?)model.StartDate ?? DBNull.Value;
            command.Parameters.Add("@TargetDate", MySqlDbType.DateTime).Value = (object?)model.TargetDate ?? DBNull.Value;
            command.Parameters.Add("@SDLCStage", MySqlDbType.Int32).Value = (object?)model.SDLCStageId ?? DBNull.Value;
            command.Parameters.Add("@PercentageDone", MySqlDbType.Decimal).Value = (object?)model.PercentageDone ?? DBNull.Value;
            command.Parameters.Add("@BITBucketRepo", MySqlDbType.VarChar).Value = (object?)model.BITBucketRepo ?? DBNull.Value;
            command.Parameters.Add("@SalesTeamId", MySqlDbType.Int32).Value = (object?)model.SalesTeamId ?? DBNull.Value;
            command.Parameters.Add("@SalesAM", MySqlDbType.VarChar).Value = (object?)model.SalesAM ?? DBNull.Value;
            command.Parameters.Add("@SalesManager", MySqlDbType.VarChar).Value = (object?)model.SalesManager ?? DBNull.Value;
            command.Parameters.Add("@SalesEngineer", MySqlDbType.VarChar).Value = (object?)model.SalesEngineer ?? DBNull.Value;
            command.Parameters.Add("@UATDate", MySqlDbType.DateTime).Value = (object?)model.UATDate ?? DBNull.Value;
            command.Parameters.Add("@VADate", MySqlDbType.DateTime).Value = (object?)model.VADate ?? DBNull.Value;
            command.Parameters.Add("@LaunchedDate", MySqlDbType.DateTime).Value = (object?)model.LaunchedDate ?? DBNull.Value;
            command.Parameters.Add("@PlatformOTC", MySqlDbType.Decimal).Value = (object?)model.PlatformOTC ?? DBNull.Value;
            command.Parameters.Add("@PlatformMRC", MySqlDbType.Decimal).Value = (object?)model.PlatformMRC ?? DBNull.Value;
            command.Parameters.Add("@SoftwareValue", MySqlDbType.Decimal).Value = (object?)model.SoftwareValue ?? DBNull.Value;
            command.Parameters.Add("@ContractPeriod", MySqlDbType.VarChar).Value = (object?)model.ContractPeriod ?? DBNull.Value;
            command.Parameters.Add("@SLA", MySqlDbType.VarChar).Value = (object?)model.SLA ?? DBNull.Value;
            command.Parameters.Add("@DPOHandoverDate", MySqlDbType.DateTime).Value = (object?)model.DPOHandoverDate ?? DBNull.Value;
            command.Parameters.Add("@DPOHandoverComment", MySqlDbType.VarChar).Value = (object?)model.DPOHandoverComment ?? DBNull.Value;

            // Note: removed BackupOfficer1Id / BackupOfficer2Id parameter bindings since DB doesn't have those columns

            command.Parameters.Add("@SSLCertificateExpDate", MySqlDbType.DateTime).Value = (object?)model.SSLCertificateExpDate ?? DBNull.Value;
            command.Parameters.Add("@BillingDate", MySqlDbType.DateTime).Value = (object?)model.BillingDate ?? DBNull.Value;
            command.Parameters.Add("@ProposalUploaded", MySqlDbType.VarChar).Value = (object?)model.ProposalUploaded ?? DBNull.Value;
            command.Parameters.Add("@IncentiveEarned", MySqlDbType.Decimal).Value = (object?)model.IncentiveEarned ?? DBNull.Value;
            command.Parameters.Add("@IncentiveShare", MySqlDbType.Decimal).Value = (object?)model.IncentiveShare ?? DBNull.Value;
        }

        #endregion
    }
}
