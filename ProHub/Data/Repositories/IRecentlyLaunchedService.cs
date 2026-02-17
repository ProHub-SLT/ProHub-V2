using ProHub.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using ProHub.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace PROHUB.Data
{
    public interface IRecentlyLaunchedService
    {
        
        Task<List<InternalPlatform>> GetRecentlyLaunchedAsync();
        Task<InternalPlatform?> GetByIdAsync(int id);
        Task<List<InternalPlatform>> GetAllLaunchedAsync();
    }


    public class RecentlyLaunchedDataAccess : IRecentlyLaunchedService
    {
        private readonly string _connectionString;
        private readonly ILogger<RecentlyLaunchedDataAccess> _logger;

        public RecentlyLaunchedDataAccess(IConfiguration configuration, ILogger<RecentlyLaunchedDataAccess> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection", "Connection string is not configured.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<InternalPlatform>> GetRecentlyLaunchedAsync()
        {
            var solutions = new List<InternalPlatform>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            //  Filter by Current Year (YEAR(isol.LaunchedDate) = YEAR(NOW()))
            const string query = @"
        SELECT isol.*, emp.Emp_Name AS DevelopedByName, sp.Phase AS SDLCPhaseName,
               te.EndUserType AS EndUserTypeName, parent.App_Name AS MainAppName,
               pp.ParentProjectGroup AS ParentProjectGroupName
        FROM internal_platforms isol
        LEFT JOIN employee emp ON isol.Developed_By = emp.Emp_ID
        LEFT JOIN sdlcphas sp ON isol.SDLCPhase = sp.ID
        LEFT JOIN targetenduser te ON isol.EndUserType = te.ID
        LEFT JOIN internal_platforms parent ON isol.MainAppID = parent.ID
        LEFT JOIN parentproject pp ON isol.ParentProjectID = pp.ParentProjectID
        WHERE isol.LaunchedDate IS NOT NULL 
          AND YEAR(isol.LaunchedDate) = YEAR(NOW())
        ORDER BY isol.LaunchedDate DESC;";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) { solutions.Add(MapReaderToSolution(reader)); }
            return solutions;
        }

        public async Task<List<InternalPlatform>> GetAllLaunchedAsync()
        {
            // All Launched Projects for Export
            var solutions = new List<InternalPlatform>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT isol.*, emp.Emp_Name AS DevelopedByName, sp.Phase AS SDLCPhaseName,
                       te.EndUserType AS EndUserTypeName, parent.App_Name AS MainAppName,
                       pp.ParentProjectGroup AS ParentProjectGroupName
                FROM internal_platforms isol
                LEFT JOIN employee emp ON isol.Developed_By = emp.Emp_ID
                LEFT JOIN sdlcphas sp ON isol.SDLCPhase = sp.ID
                LEFT JOIN targetenduser te ON isol.EndUserType = te.ID
                LEFT JOIN internal_platforms parent ON isol.MainAppID = parent.ID
                LEFT JOIN parentproject pp ON isol.ParentProjectID = pp.ParentProjectID
                WHERE isol.LaunchedDate IS NOT NULL
                ORDER BY isol.LaunchedDate DESC;";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) { solutions.Add(MapReaderToSolution(reader)); }
            return solutions;
        }

        public async Task<InternalPlatform?> GetByIdAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT isol.*, emp.Emp_Name AS DevelopedByName, sp.Phase AS SDLCPhaseName,
                       te.EndUserType AS EndUserTypeName, parent.App_Name AS MainAppName,
                       pp.ParentProjectGroup AS ParentProjectGroupName
                FROM internal_platforms isol
                LEFT JOIN employee emp ON isol.Developed_By = emp.Emp_ID
                LEFT JOIN sdlcphas sp ON isol.SDLCPhase = sp.ID
                LEFT JOIN targetenduser te ON isol.EndUserType = te.ID
                LEFT JOIN internal_platforms parent ON isol.MainAppID = parent.ID
                LEFT JOIN parentproject pp ON isol.ParentProjectID = pp.ParentProjectID
                WHERE isol.ID = @ID;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReaderToSolution(reader) : null;
        }

        // Helper Method 
        private InternalPlatform MapReaderToSolution(DbDataReader reader)
        {
            string? SafeGetString(string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? null : reader.GetString(i); }
            int? SafeGetInt(string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? (int?)null : reader.GetInt32(i); }
            decimal? SafeGetDecimal(string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? (decimal?)null : reader.GetDecimal(i); }
            DateTime? SafeGetDateTime(string name) { var i = reader.GetOrdinal(name); return reader.IsDBNull(i) ? (DateTime?)null : reader.GetDateTime(i); }

            return new InternalPlatform
            {
                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                AppName = SafeGetString("App_Name"),
                DevelopedById = SafeGetInt("Developed_By"),
                DevelopedTeam = SafeGetString("Developed_Team"),
                StartDate = SafeGetDateTime("StartDate"),
                TargetDate = SafeGetDateTime("TargetDate"),
                BitBucketRepo = SafeGetString("Bit_bucket_repo"),
                SDLCPhaseId = SafeGetInt("SDLCPhase"),
                PercentageDone = SafeGetDecimal("PercentageDone"),
                Status = SafeGetString("Status"),
                StatusDate = SafeGetDateTime("StatusDate"),
                BusOwner = SafeGetString("Bus_Owner"),
                AppCategory = SafeGetString("App_Category"),
                Scope = SafeGetString("Scope"),
                AppIP = SafeGetString("App_IP"),
                AppURL = SafeGetString("App_URL"),
                AppUsers = SafeGetString("App_Users"),
                UATDate = SafeGetDateTime("UATDate"),
                IntegratedApps = SafeGetString("Integrated_Apps"),
                DR = SafeGetString("DR"),
                LaunchedDate = SafeGetDateTime("LaunchedDate"),
                VADate = SafeGetDateTime("VADate"),
                WAF = SafeGetString("WAF"),
                BackupOfficer1Id = SafeGetInt("BackupOfficer_1"),
                BackupOfficer2Id = SafeGetInt("BackupOfficer_2"),
                APPOwner = SafeGetString("APP_OP_Owner"),
                AppBusinessOwner = SafeGetString("App_Business_Owner"),
                Price = SafeGetDecimal("Price"),
                EndUserTypeId = SafeGetInt("EndUserType"),
                RequestNo = SafeGetString("RequestNo"),
                ParentProjectID = SafeGetInt("ParentProjectID"),
                SLA = SafeGetString("SLA"),
                MainAppID = SafeGetInt("MainAppID"),
                SSLCertificateExpDate = SafeGetDateTime("SSLCertificateExpDate"),
                DevelopedByName = SafeGetString("DevelopedByName"),
                SDLCPhaseName = SafeGetString("SDLCPhaseName"),
                EndUserTypeName = SafeGetString("EndUserTypeName"),
                MainAppName = SafeGetString("MainAppName"),
                ParentProjectGroupName = SafeGetString("ParentProjectGroupName")
            };
        }
    }
}
