using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ProHub.Models;

namespace PROHUB.Data
{
    public interface IInternalSolutionService
    {
        Task<List<InternalPlatform>> GetAllAsync();
        Task<InternalPlatform?> GetByIdAsync(int id);
        Task<int> CreateAsync(InternalPlatform solution);
        Task<bool> UpdateAsync(InternalPlatform solution);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<SDLCPhase>> GetSdlcPhasesAsync();
        Task<List<TargetEndUser>> GetEndUserTypesAsync();
        Task<List<InternalPlatform>> GetMainApplicationsAsync();
        Task<List<ParentProject>> GetParentProjectsAsync();
        Task AddCommentAsync(int solutionId, string comment, int? updatedBy);
    }



    public class InternalSolutionDataAccess : IInternalSolutionService
    {
        private readonly string _connectionString;
        private readonly ILogger<InternalSolutionDataAccess> _logger;

        public InternalSolutionDataAccess(IConfiguration configuration, ILogger<InternalSolutionDataAccess> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException("DefaultConnection", "Connection string is not configured in appsettings.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }



        public async Task<List<InternalPlatform>> GetAllAsync()
        {
            var solutions = new List<InternalPlatform>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT isol.*, emp.Emp_Name AS DevelopedByName, sp.Phase AS SDLCPhaseName,
                       te.EndUserType AS EndUserTypeName, parent.App_Name AS MainAppName,
                       pp.ParentProjectGroup AS ParentProjectGroupName
                FROM internal_platforms isol
                LEFT JOIN employee emp ON isol.Developed_By = emp.Emp_ID
                LEFT JOIN SDLCPhas sp ON isol.SDLCPhase = sp.ID
                LEFT JOIN Targetenduser te ON isol.EndUserType = te.ID
                LEFT JOIN internal_platforms parent ON isol.MainAppID = parent.ID
                LEFT JOIN ParentProject pp ON isol.ParentProjectID = pp.ParentProjectID;";

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
                LEFT JOIN SDLCPhas sp ON isol.SDLCPhase = sp.ID
                LEFT JOIN Targetenduser te ON isol.EndUserType = te.ID
                LEFT JOIN internal_platforms parent ON isol.MainAppID = parent.ID
                LEFT JOIN ParentProject pp ON isol.ParentProjectID = pp.ParentProjectID
                WHERE isol.ID = @ID;";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReaderToSolution(reader) : null;
        }

        public async Task<int> CreateAsync(InternalPlatform solution)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO internal_platforms (
                    App_Name, Developed_By, Developed_Team, StartDate, TargetDate,
                    Bit_bucket_repo, SDLCPhase, PercentageDone, Status, StatusDate,
                    Bus_Owner, App_Category, Scope, App_IP, App_URL, App_Users,
                    UATDate, Integrated_Apps, DR, LaunchedDate, VADate, WAF,
                    APP_OP_Owner, App_Business_Owner, Price, EndUserType, RequestNo,
                    ParentProjectID, SLA, MainAppID, SSLCertificateExpDate,
                    BackupOfficer_1, BackupOfficer_2
                ) VALUES (
                    @App_Name, @Developed_By, @Developed_Team, @StartDate, @TargetDate,
                    @Bit_bucket_repo, @SDLCPhase, @PercentageDone, @Status, @StatusDate,
                    @Bus_Owner, @App_Category, @Scope, @App_IP, @App_URL, @App_Users,
                    @UATDate, @Integrated_Apps, @DR, @LaunchedDate, @VADate, @WAF,
                    @APP_OP_Owner, @App_Business_Owner, @Price, @EndUserType, @RequestNo,
                    @ParentProjectID, @SLA, @MainAppID, @SSLCertificateExpDate,
                    @BackupOfficer_1, @BackupOfficer_2
                );
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, solution);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // ---------------------------------------------------------
        //  MODIFIED UPDATE ASYNC METHOD
        // ---------------------------------------------------------
        public async Task<bool> UpdateAsync(InternalPlatform solution)
        {
            if (solution == null) throw new ArgumentNullException(nameof(solution));
            if (solution.Id <= 0) throw new ArgumentException("Solution ID must be valid.");

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 1. Update the Main Solution Table
                const string updateQuery = @"
                    UPDATE internal_platforms SET
                        App_Name = @App_Name, Developed_By = @Developed_By, Developed_Team = @Developed_Team,
                        StartDate = @StartDate, TargetDate = @TargetDate, Bit_bucket_repo = @Bit_bucket_repo,
                        SDLCPhase = @SDLCPhase, PercentageDone = @PercentageDone, Status = @Status,
                        StatusDate = @StatusDate, Bus_Owner = @Bus_Owner, App_Category = @App_Category,
                        Scope = @Scope, App_IP = @App_IP, App_URL = @App_URL, App_Users = @App_Users,
                        UATDate = @UATDate, Integrated_Apps = @Integrated_Apps, DR = @DR,
                        LaunchedDate = @LaunchedDate, VADate = @VADate, WAF = @WAF,
                        APP_OP_Owner = @APP_OP_Owner, App_Business_Owner = @App_Business_Owner, Price = @Price,
                        EndUserType = @EndUserType, RequestNo = @RequestNo, ParentProjectID = @ParentProjectID,
                        SLA = @SLA, MainAppID = @MainAppID, SSLCertificateExpDate = @SSLCertificateExpDate,
                        BackupOfficer_1 = @BackupOfficer_1, BackupOfficer_2 = @BackupOfficer_2
                    WHERE ID = @ID;";

                using (var command = new MySqlCommand(updateQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@ID", solution.Id);
                    AddParameters(command, solution);
                    await command.ExecuteNonQueryAsync();
                }

                // 2. Check if there is a new comment to save
                if (!string.IsNullOrWhiteSpace(solution.Comment))
                {
                    // Note: Here we are using 'DevelopedById' as the person who updated it.
                    // If you have a logged-in User ID, pass that instead.
                    await AddCommentToDbAsync(connection, transaction, solution.Id, solution.Comment, solution.DevelopedById);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating solution {Id}", solution.Id);
                return false;
            }
        }

        // ---------------------------------------------------------
        //  NEW INTERFACE METHOD IMPLEMENTATION
        // ---------------------------------------------------------
        public async Task AddCommentAsync(int solutionId, string comment, int? updatedBy)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            // Pass null for transaction since this is a standalone operation
            await AddCommentToDbAsync(connection, null, solutionId, comment, updatedBy);
        }

        // ---------------------------------------------------------
        //  PRIVATE HELPER TO INSERT COMMENT
        // ---------------------------------------------------------
        private async Task AddCommentToDbAsync(MySqlConnection connection, MySqlTransaction? transaction, int solutionId, string comment, int? updatedBy)
        {
            const string insertQuery = @"
                INSERT INTO internal_project_comments 
                (Solution_ID, Comment, Updated_By, Updated_Time) 
                VALUES 
                (@SolutionId, @Comment, @UpdatedBy, @UpdatedTime);";

            using var cmd = new MySqlCommand(insertQuery, connection, transaction);
            cmd.Parameters.AddWithValue("@SolutionId", solutionId);
            cmd.Parameters.AddWithValue("@Comment", comment);
            cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@UpdatedTime", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            const string query = "DELETE FROM internal_platforms WHERE ID = @ID;";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            const string query = "SELECT COUNT(1) FROM internal_platforms WHERE ID = @ID;";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ID", id);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        // ... [Existing Helper Get Methods (Employees, SDLC, etc.) remain unchanged] ...
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand(@"
                SELECT e.Emp_ID, e.Emp_Name
                FROM employee e
                LEFT JOIN empgroup g ON e.GroupID = g.GroupID
                WHERE g.GroupName IS NULL
                   OR g.GroupName <> 'Inactive'
                ORDER BY e.Emp_Name;
            ", conn);

            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new Employee
                {
                    EmpId = rdr.GetInt32(0),
                    EmpName = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1)
                });
            }

            return list;
        }


        public async Task<List<SDLCPhase>> GetSdlcPhasesAsync()
        {
            var list = new List<SDLCPhase>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, Phase FROM SDLCPhas ORDER BY OrderSeq, Phase", conn);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new SDLCPhase { Id = rdr.GetInt32(0), Phase = rdr.IsDBNull(1) ? "" : rdr.GetString(1) });
            }
            return list;
        }

        public async Task<List<TargetEndUser>> GetEndUserTypesAsync()
        {
            var list = new List<TargetEndUser>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, EndUserType FROM Targetenduser ORDER BY EndUserType", conn);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new TargetEndUser { ID = rdr.GetInt32(0), EndUserType = rdr.IsDBNull(1) ? "" : rdr.GetString(1) });
            }
            return list;
        }

        public async Task<List<InternalPlatform>> GetMainApplicationsAsync()
        {
            var list = new List<InternalPlatform>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ID, App_Name FROM internal_platforms ORDER BY App_Name", conn);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new InternalPlatform { Id = rdr.GetInt32(0), AppName = rdr.IsDBNull(1) ? "" : rdr.GetString(1) });
            }
            return list;
        }

        public async Task<List<ParentProject>> GetParentProjectsAsync()
        {
            var list = new List<ParentProject>();
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = new MySqlCommand("SELECT ParentProjectID, ParentProjectGroup FROM ParentProject ORDER BY ParentProjectGroup", conn);
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                list.Add(new ParentProject { ParentProjectID = rdr.GetInt32(0), ParentProjectGroup = rdr.IsDBNull(1) ? "" : rdr.GetString(1) });
            }
            return list;
        }

        // --- Helpers ---

        private void AddParameters(MySqlCommand command, InternalPlatform solution)
        {
            command.Parameters.AddWithValue("@App_Name", solution.AppName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Developed_By", solution.DevelopedById ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Developed_Team", solution.DevelopedTeam ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", solution.StartDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TargetDate", solution.TargetDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Bit_bucket_repo", solution.BitBucketRepo ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SDLCPhase", solution.SDLCPhaseId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@PercentageDone", solution.PercentageDone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", solution.Status ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StatusDate", solution.StatusDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Bus_Owner", solution.BusOwner ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@App_Category", solution.AppCategory ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Scope", solution.Scope ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@App_IP", solution.AppIP ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@App_URL", solution.AppURL ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@App_Users", solution.AppUsers ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UATDate", solution.UATDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Integrated_Apps", solution.IntegratedApps ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DR", solution.DR ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LaunchedDate", solution.LaunchedDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@VADate", solution.VADate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@WAF", solution.WAF ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@APP_OP_Owner", solution.APPOwner ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@App_Business_Owner", solution.AppBusinessOwner ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Price", solution.Price ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EndUserType", solution.EndUserTypeId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RequestNo", solution.RequestNo ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ParentProjectID", solution.ParentProjectID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SLA", solution.SLA ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MainAppID", solution.MainAppID ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@SSLCertificateExpDate", solution.SSLCertificateExpDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BackupOfficer_1", solution.BackupOfficer1Id ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BackupOfficer_2", solution.BackupOfficer2Id ?? (object)DBNull.Value);
        }

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
