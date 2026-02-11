using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using PROHUB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PROHUB.Data
{
    // --- Interface ---
    public interface IInternalSolutionInprogressService
    {
        Task<List<InternalPlatform>> GetInProgressSolutionsAsync(string searchTerm, string tabFilter);
        Task<InternalPlatform?> GetByIdAsync(int id);
        Task<int> CreateAsync(InternalPlatform platform);
        Task<bool> UpdateAsync(InternalPlatform platform);
        Task<bool> DeleteAsync(int id);
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<SDLCPhase>> GetSdlcPhasesAsync();
        Task<List<TargetEndUser>> GetEndUserTypesAsync();
        Task<List<ParentProject>> GetParentProjectsAsync();
        Task<List<InternalPlatform>> GetAllInternalPlatformsAsync();
        Task AddCommentAsync(int solutionId, string comment, int? updatedBy);
    }

    // --- Implementation ---
    public class InternalSolutionInprogressDataAccess : IInternalSolutionInprogressService
    {
        private readonly string _connectionString;

        public InternalSolutionInprogressDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        // 1. Get List
        public async Task<List<InternalPlatform>> GetInProgressSolutionsAsync(string searchTerm, string tabFilter)
        {
            var list = new List<InternalPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            // --- SQL Query Updated ---
           
            string query = @"
        SELECT ip.*, 
               e.Emp_Name AS DevelopedByName, 
               sp.Phase AS SDLCPhaseName, 
               pr.ParentProjectGroup AS ParentProjectName,
               parent.App_Name AS MainAppName,  
               (SELECT COUNT(*) FROM Internal_Project_Comments WHERE Solution_ID = ip.ID) AS CommentCount
        FROM internal_platforms ip
        LEFT JOIN employee e ON ip.Developed_By = e.Emp_ID
        LEFT JOIN SDLCPhas sp ON ip.SDLCPhase = sp.ID
        LEFT JOIN parentproject pr ON ip.ParentProjectID = pr.ParentProjectID
        LEFT JOIN internal_platforms parent ON ip.MainAppID = parent.ID 
        WHERE sp.Phase NOT IN ('Maintenance', 'Retired', 'Abandoned')";

            // --- TAB FILTER LOGIC ---
            if (!string.IsNullOrEmpty(tabFilter))
            {
                if (tabFilter.ToLower() == "level1")
                {
                    query += " AND ip.Status = 'Level 1'";
                }
                else if (tabFilter.ToLower() == "other")
                {
                    query += " AND (ip.Status = 'Level 2' OR ip.Status IS NULL OR ip.Status = '')";
                }
            }

            // --- SEARCH LOGIC ---
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (ip.App_Name LIKE @SearchTerm OR e.Emp_Name LIKE @SearchTerm OR ip.Bus_Owner LIKE @SearchTerm OR pr.ParentProjectGroup LIKE @SearchTerm)";
            }

            query += " ORDER BY ip.StartDate DESC";

            using var command = new MySqlCommand(query, connection);
            if (!string.IsNullOrEmpty(searchTerm)) command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var item = MapReaderToModel(reader);
                item.CommentCount = reader.GetInt32(reader.GetOrdinal("CommentCount"));
                list.Add(item);
            }
            return list;
        }

        // 2. Get By ID
        public async Task<InternalPlatform?> GetByIdAsync(int id)
        {
            InternalPlatform? solution = null;

            using var connection = GetConnection();
            await connection.OpenAsync();

            // Step A: Get Main Solution Data

            const string query = @"
                SELECT ip.*, 
                       e.Emp_Name AS DevelopedByName, 
                       e.Emp_Email AS DeveloperEmail,
                       sp.Phase AS SDLCPhaseName, 
                       pr.ParentProjectGroup AS ParentProjectName,
                       parent.App_Name AS MainAppName,
                       teu.EndUserType AS EndUserTypeName, -- Added EndUserType
                       bo1.Emp_Name AS BackupOfficer1Name, -- Added Backup Officer 1
                       bo2.Emp_Name AS BackupOfficer2Name  -- Added Backup Officer 2
                FROM internal_platforms ip
                LEFT JOIN employee e ON ip.Developed_By = e.Emp_ID
                LEFT JOIN SDLCPhas sp ON ip.SDLCPhase = sp.ID
                LEFT JOIN parentproject pr ON ip.ParentProjectID = pr.ParentProjectID
                LEFT JOIN internal_platforms parent ON ip.MainAppID = parent.ID
                LEFT JOIN targetenduser teu ON ip.EndUserType = teu.ID -- Join for End User
                LEFT JOIN employee bo1 ON ip.BackupOfficer_1 = bo1.Emp_ID -- Join for BO1
                LEFT JOIN employee bo2 ON ip.BackupOfficer_2 = bo2.Emp_ID -- Join for BO2
                WHERE ip.ID = @Id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    solution = MapReaderToModel(reader);

                    solution.DeveloperEmail = reader.IsDBNull(reader.GetOrdinal("DeveloperEmail")) ? "Not specified" : reader.GetString("DeveloperEmail");
                    solution.EndUserTypeName = reader.IsDBNull(reader.GetOrdinal("EndUserTypeName")) ? "Not specified" : reader.GetString("EndUserTypeName");
                    solution.BackupOfficer1Name = reader.IsDBNull(reader.GetOrdinal("BackupOfficer1Name")) ? null : reader.GetString("BackupOfficer1Name");
                    solution.BackupOfficer2Name = reader.IsDBNull(reader.GetOrdinal("BackupOfficer2Name")) ? null : reader.GetString("BackupOfficer2Name");
                }
            }

            // Step B: Get Comments History
            if (solution != null)
            {
                solution.ProjectComments = new List<InternalProjectComment>();
                const string commentQuery = @"
                    SELECT c.ID, c.Comment, c.Updated_Time, e.Emp_Name AS UpdatedByName
                    FROM Internal_Project_Comments c
                    LEFT JOIN employee e ON c.Updated_By = e.Emp_ID
                    WHERE c.Solution_ID = @SolutionId
                    ORDER BY c.Updated_Time DESC";

                using (var cmdComments = new MySqlCommand(commentQuery, connection))
                {
                    cmdComments.Parameters.AddWithValue("@SolutionId", id);
                    using var commentReader = await cmdComments.ExecuteReaderAsync();
                    while (await commentReader.ReadAsync())
                    {
                        solution.ProjectComments.Add(new InternalProjectComment
                        {
                            ID = commentReader.GetInt32("ID"),
                            Comment = commentReader.GetString("Comment"),
                            Updated_Time = commentReader.GetDateTime("Updated_Time"),
                            UpdatedByEmployee = new Employee
                            {
                                EmpName = commentReader.IsDBNull(commentReader.GetOrdinal("UpdatedByName")) ? "Unknown" : commentReader.GetString("UpdatedByName")
                            }
                        });
                    }
                }
            }

            return solution;
        }

        // 3. Create 
        public async Task<int> CreateAsync(InternalPlatform platform)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO internal_platforms (
                    App_Name, Developed_By, Developed_Team, StartDate, TargetDate, BitBucket, 
                    Bit_bucket_repo, SDLCPhase, PercentageDone, Status, StatusDate, Bus_Owner, 
                    App_Category, Scope, App_IP, App_URL, App_Users, UATDate, Integrated_Apps, 
                    DR, LaunchedDate, VADate, WAF, APP_OP_Owner, App_Business_Owner, Price, 
                    EndUserType, RequestNo, ParentProjectID, SLA, BackupOfficer_1, BackupOfficer_2, 
                    MainAppID, SSLCertificateExpDate
                ) VALUES (
                    @AppName, @DevelopedById, @DevelopedTeam, @StartDate, @TargetDate, @BitBucket, 
                    @BitBucketRepo, @SDLCPhaseId, @PercentageDone, @Status, @StatusDate, @BusOwner, 
                    @AppCategory, @Scope, @AppIP, @AppURL, @AppUsers, @UATDate, @IntegratedApps, 
                    @DR, @LaunchedDate, @VADate, @WAF, @APPOwner, @AppBusinessOwner, @Price, 
                    @EndUserTypeId, @RequestNo, @ParentProjectID, @SLA, @BackupOfficer1Id, @BackupOfficer2Id, 
                    @MainAppID, @SSLCertificateExpDate
                ); SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, platform);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
        }

        // 4. Update (Includes Transaction for Comments)
        public async Task<bool> UpdateAsync(InternalPlatform platform)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();

            try
            {

                const string updateQuery = @"
                    UPDATE internal_platforms SET
                        App_Name = @AppName, Developed_By = @DevelopedById, Developed_Team = @DevelopedTeam, 
                        StartDate = @StartDate, TargetDate = @TargetDate, BitBucket = @BitBucket, 
                        Bit_bucket_repo = @BitBucketRepo, SDLCPhase = @SDLCPhaseId, PercentageDone = @PercentageDone, 
                        Status = @Status, StatusDate = @StatusDate, Bus_Owner = @BusOwner, 
                        App_Category = @AppCategory, Scope = @Scope, App_IP = @AppIP, App_URL = @AppURL, 
                        App_Users = @AppUsers, UATDate = @UATDate, Integrated_Apps = @IntegratedApps, 
                        DR = @DR, LaunchedDate = @LaunchedDate, VADate = @VADate, WAF = @WAF, 
                        APP_OP_Owner = @APPOwner, App_Business_Owner = @AppBusinessOwner, Price = @Price, 
                        EndUserType = @EndUserTypeId, RequestNo = @RequestNo, ParentProjectID = @ParentProjectID, 
                        SLA = @SLA, BackupOfficer_1 = @BackupOfficer1Id, BackupOfficer_2 = @BackupOfficer2Id, 
                        MainAppID = @MainAppID, SSLCertificateExpDate = @SSLCertificateExpDate
                    WHERE ID = @Id";

                using (var command = new MySqlCommand(updateQuery, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", platform.Id);
                    AddParameters(command, platform);
                    await command.ExecuteNonQueryAsync();
                }

                // Step 2: Insert Comment (If user typed one)
                if (!string.IsNullOrWhiteSpace(platform.Comment))
                {
                    const string commentQuery = @"
                        INSERT INTO Internal_Project_Comments 
                        (Solution_ID, Comment, Updated_By, Updated_Time) 
                        VALUES 
                        (@SolutionId, @Comment, @UpdatedBy, NOW())";

                    using (var commentCmd = new MySqlCommand(commentQuery, connection, transaction))
                    {
                        commentCmd.Parameters.AddWithValue("@SolutionId", platform.Id);
                        commentCmd.Parameters.AddWithValue("@Comment", platform.Comment);
                        commentCmd.Parameters.AddWithValue("@UpdatedBy", (object)platform.DevelopedById ?? DBNull.Value);

                        await commentCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // 5. Delete 
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand("DELETE FROM internal_platforms WHERE ID = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        // 6. Add Single Comment
        public async Task AddCommentAsync(int solutionId, string comment, int? updatedBy)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO Internal_Project_Comments (Solution_ID, Comment, Updated_By, Updated_Time) 
                VALUES (@SolutionId, @Comment, @UpdatedBy, NOW())";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@SolutionId", solutionId);
            command.Parameters.AddWithValue("@Comment", comment);
            command.Parameters.AddWithValue("@UpdatedBy", (object)updatedBy ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        // --- Dropdowns 
        public async Task<List<Employee>> GetEmployeesAsync() =>
        await GetDropdownListAsync(
            @"SELECT e.Emp_ID, e.Emp_Name
          FROM employee e
          LEFT JOIN EmpGroup g ON e.GroupID = g.GroupID
          WHERE g.GroupName IS NULL
             OR g.GroupName <> 'Inactive'
          ORDER BY e.Emp_Name",
            r => new Employee
            {
                EmpId = r.GetInt32(0),
                EmpName = r.IsDBNull(1) ? string.Empty : r.GetString(1)
            });

        public async Task<List<SDLCPhase>> GetSdlcPhasesAsync() => await GetDropdownListAsync("SELECT ID, Phase FROM sdlcphas ORDER BY OrderSeq, Phase", r => new SDLCPhase { Id = r.GetInt32(0), Phase = r.GetString(1) });
        public async Task<List<TargetEndUser>> GetEndUserTypesAsync() => await GetDropdownListAsync("SELECT ID, EndUserType FROM targetenduser ORDER BY EndUserType", r => new TargetEndUser { ID = r.GetInt32(0), EndUserType = r.GetString(1) });
        public async Task<List<ParentProject>> GetParentProjectsAsync() => await GetDropdownListAsync("SELECT ParentProjectID, ParentProjectGroup FROM parentproject ORDER BY ParentProjectGroup", r => new ParentProject { ParentProjectID = r.GetInt32(0), ParentProjectGroup = r.GetString(1) });
        public async Task<List<InternalPlatform>> GetAllInternalPlatformsAsync() => await GetDropdownListAsync("SELECT ID, App_Name FROM internal_platforms ORDER BY App_Name", r => new InternalPlatform { Id = r.GetInt32(0), AppName = r.GetString(1) });

        private async Task<List<T>> GetDropdownListAsync<T>(string query, Func<IDataReader, T> map)
        {
            var list = new List<T>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(map(reader));
            return list;
        }

        // --- Helpers ---
        private InternalPlatform MapReaderToModel(IDataReader r)
        {
            return new InternalPlatform
            {
                Id = GetInt32(r, "ID"),
                AppName = GetNullableString(r, "App_Name"),
                MainAppName = GetNullableString(r, "MainAppName"), 
                DevelopedById = GetNullableInt32(r, "Developed_By"),
                DevelopedTeam = GetNullableString(r, "Developed_Team"),
                DevelopedByName = GetNullableString(r, "DevelopedByName"), // Fixes "Developed By"
                SDLCPhaseName = GetNullableString(r, "SDLCPhaseName"),     // Fixes "SDLC Stage" & Header Badge
                StartDate = GetNullableDateTime(r, "StartDate"),
                TargetDate = GetNullableDateTime(r, "TargetDate"),
                BitBucket = GetNullableString(r, "BitBucket"),
                BitBucketRepo = GetNullableString(r, "Bit_bucket_repo"),
                SDLCPhaseId = GetNullableInt32(r, "SDLCPhase"),
                PercentageDone = GetNullableDecimal(r, "PercentageDone"),
                Status = GetNullableString(r, "Status"),
                StatusDate = GetNullableDateTime(r, "StatusDate"),
                BusOwner = GetNullableString(r, "Bus_Owner"),
                AppCategory = GetNullableString(r, "App_Category"),
                Scope = GetNullableString(r, "Scope"),
                AppIP = GetNullableString(r, "App_IP"),
                AppURL = GetNullableString(r, "App_URL"),
                AppUsers = GetNullableString(r, "App_Users"),
                UATDate = GetNullableDateTime(r, "UATDate"),
                IntegratedApps = GetNullableString(r, "Integrated_Apps"),
                DR = GetNullableString(r, "DR"),
                LaunchedDate = GetNullableDateTime(r, "LaunchedDate"),
                VADate = GetNullableDateTime(r, "VADate"),
                WAF = GetNullableString(r, "WAF"),
                APPOwner = GetNullableString(r, "APP_OP_Owner"),
                AppBusinessOwner = GetNullableString(r, "App_Business_Owner"),
                Price = GetNullableDecimal(r, "Price"),
                EndUserTypeId = GetNullableInt32(r, "EndUserType"),
                RequestNo = GetNullableString(r, "RequestNo"),
                ParentProjectID = GetNullableInt32(r, "ParentProjectID"),
                SLA = GetNullableString(r, "SLA"),
                BackupOfficer1Id = GetNullableInt32(r, "BackupOfficer_1"),
                BackupOfficer2Id = GetNullableInt32(r, "BackupOfficer_2"),
                MainAppID = GetNullableInt32(r, "MainAppID"),
                SSLCertificateExpDate = GetNullableDateTime(r, "SSLCertificateExpDate"),

                DevelopedBy = new Employee { EmpName = GetNullableString(r, "DevelopedByName") },
                SDLCPhase = new SDLCPhase { Phase = GetNullableString(r, "SDLCPhaseName") },
                ParentProject = new ParentProject { ParentProjectGroup = GetNullableString(r, "ParentProjectName") }
            };
        }

        // AddParameters Helper 
        private void AddParameters(MySqlCommand cmd, InternalPlatform p)
        {
            cmd.Parameters.AddWithValue("@AppName", p.AppName);
            cmd.Parameters.AddWithValue("@DevelopedById", (object)p.DevelopedById ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DevelopedTeam", (object)p.DevelopedTeam ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StartDate", (object)p.StartDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TargetDate", (object)p.TargetDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BitBucket", (object)p.BitBucket ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BitBucketRepo", (object)p.BitBucketRepo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SDLCPhaseId", (object)p.SDLCPhaseId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PercentageDone", (object)p.PercentageDone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object)p.Status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StatusDate", (object)p.StatusDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BusOwner", (object)p.BusOwner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppCategory", (object)p.AppCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Scope", (object)p.Scope ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppIP", (object)p.AppIP ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppURL", (object)p.AppURL ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppUsers", (object)p.AppUsers ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UATDate", (object)p.UATDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IntegratedApps", (object)p.IntegratedApps ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DR", (object)p.DR ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LaunchedDate", (object)p.LaunchedDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@VADate", (object)p.VADate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@WAF", (object)p.WAF ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@APPOwner", (object)p.APPOwner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AppBusinessOwner", (object)p.AppBusinessOwner ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Price", (object)p.Price ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@EndUserTypeId", (object)p.EndUserTypeId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@RequestNo", (object)p.RequestNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ParentProjectID", (object)p.ParentProjectID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SLA", (object)p.SLA ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BackupOfficer1Id", (object)p.BackupOfficer1Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BackupOfficer2Id", (object)p.BackupOfficer2Id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MainAppID", (object)p.MainAppID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SSLCertificateExpDate", (object)p.SSLCertificateExpDate ?? DBNull.Value);
        }

        private static int GetInt32(IDataReader r, string c) => r.IsDBNull(r.GetOrdinal(c)) ? 0 : r.GetInt32(r.GetOrdinal(c));
        private static int? GetNullableInt32(IDataReader r, string c) => r.IsDBNull(r.GetOrdinal(c)) ? null : (int?)r.GetInt32(r.GetOrdinal(c));
        private static string? GetNullableString(IDataReader r, string c) => r.IsDBNull(r.GetOrdinal(c)) ? null : r.GetString(r.GetOrdinal(c));
        private static DateTime? GetNullableDateTime(IDataReader r, string c) => r.IsDBNull(r.GetOrdinal(c)) ? null : (DateTime?)r.GetDateTime(r.GetOrdinal(c));
        private static decimal? GetNullableDecimal(IDataReader r, string c) => r.IsDBNull(r.GetOrdinal(c)) ? null : (decimal?)r.GetDecimal(r.GetOrdinal(c));
    }
}