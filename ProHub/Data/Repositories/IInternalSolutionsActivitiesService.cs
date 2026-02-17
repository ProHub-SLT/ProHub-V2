using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using PROHUB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace PROHUB.Data
{
    // ---------------------------------------------------------
    // 1. THE INTERFACE
    // ---------------------------------------------------------
    public interface IInternalSolutionsActivitiesService
    {
        Task<List<ProjectActivity>> GetAllAsync(string search = null, string sortColumn = null, string sortOrder = null, int? filterPlatformId = null);

        Task<ProjectActivity?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProjectActivity activity);
        Task<bool> UpdateAsync(ProjectActivity activity);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task AddCommentAsync(int activityId, string comment, int? updatedBy);

        // Dropdown Helpers
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<MainPlatform>> GetMainPlatformsAsync();
        Task<List<InternalPlatform>> GetInternalSolutionsAsync();
        Task<Employee?> GetEmployeeByEmailAsync(string email);
    }

    // ---------------------------------------------------------
    // 2. THE IMPLEMENTATION
    // ---------------------------------------------------------

    public class InternalSolutionsActivitiesDataAccess : IInternalSolutionsActivitiesService
    {
        private readonly string _connectionString;

        public InternalSolutionsActivitiesDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        // --- CRUD OPERATIONS ---

        public async Task<List<ProjectActivity>> GetAllAsync(string search = null, string sortColumn = null, string sortOrder = null, int? filterPlatformId = null)
        {
            var list = new List<ProjectActivity>();
            using var connection = GetConnection();
            await connection.OpenAsync();


            string query = @"
                SELECT pa.*, 
                       COALESCE(mp.Platforms, ip.App_Name) AS PlatformName,
                       COALESCE(isol.App_Name, ip.App_Name) AS SolutionName,
                       e1.Emp_Name AS CreatedByName, 
                       e2.Emp_Name AS AssignedToName, 
                       e3.Emp_Name AS UpdatedByName,
                       (SELECT Comment FROM project_comments pc WHERE pc.Activity_ID = pa.ID ORDER BY pc.ID DESC LIMIT 1) AS LatestComment
                FROM project_activities pa
                LEFT JOIN main_platforms mp ON pa.Platform_ID = mp.ID
                LEFT JOIN internal_platforms ip ON pa.Platform_ID = ip.ID
                LEFT JOIN internal_platforms isol ON pa.Solution_ID = isol.ID
                LEFT JOIN employee e1 ON pa.Created_By = e1.Emp_ID
                LEFT JOIN employee e2 ON pa.Assigned_To = e2.Emp_ID
                LEFT JOIN employee e3 ON pa.Updated_By = e3.Emp_ID
                WHERE 1=1";

            if (filterPlatformId.HasValue)
            {
                query += " AND pa.Platform_ID = @PlatformId";
            }
            else
            {
                // Default fallback if needed, or remove this else block to show all if not filtered.
                // Keeping original behavior for safety if not specified, assuming 1 was intended default.
                query += " AND pa.Platform_ID = 1"; 
            }

            // 1. Search Logic
            bool hasSearch = !string.IsNullOrEmpty(search);
            if (hasSearch)
            {

                query += @" AND (
                                COALESCE(isol.App_Name, ip.App_Name) LIKE @Search 
                                OR pa.Description LIKE @Search
                                OR e1.Emp_Name LIKE @Search
                                OR e2.Emp_Name LIKE @Search
                                OR e3.Emp_Name LIKE @Search
                            )";
            }

            // 2. Sort Logic
            var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Solution", "SolutionName" },
                { "CreatedBy", "CreatedByName" },
                { "AssignedTo", "AssignedToName" },
                { "Status", "pa.Status" },
                { "Updatedby", "UpdatedByName" },
                { "CreatedTime", "pa.Created_Time" }
            };

            string orderBy = "pa.ID";
            if (!string.IsNullOrEmpty(sortColumn) && sortMap.ContainsKey(sortColumn))
            {
                orderBy = sortMap[sortColumn];
            }

            string dir = (sortOrder?.ToLower() == "asc") ? "ASC" : "DESC";
            query += $" ORDER BY {orderBy} {dir}";

            using var command = new MySqlCommand(query, connection);

            if (hasSearch)
            {
                command.Parameters.AddWithValue("@Search", $"%{search}%");
            }

            if (filterPlatformId.HasValue)
            {
                command.Parameters.AddWithValue("@PlatformId", filterPlatformId.Value);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapReaderToModel(reader));
            }
            return list;
        }

        public async Task<ProjectActivity?> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT pa.*, 
                       COALESCE(mp.Platforms, ip.App_Name) AS PlatformName,
                       COALESCE(isol.App_Name, ip.App_Name) AS SolutionName,
                       e1.Emp_Name AS CreatedByName, 
                       e2.Emp_Name AS AssignedToName, 
                       e3.Emp_Name AS UpdatedByName,
                       (SELECT Comment FROM project_comments pc WHERE pc.Activity_ID = pa.ID ORDER BY pc.ID DESC LIMIT 1) AS LatestComment
                FROM project_activities pa
                LEFT JOIN main_platforms mp ON pa.Platform_ID = mp.ID
                LEFT JOIN internal_platforms ip ON pa.Platform_ID = ip.ID
                LEFT JOIN internal_platforms isol ON pa.Solution_ID = isol.ID
                LEFT JOIN employee e1 ON pa.Created_By = e1.Emp_ID
                LEFT JOIN employee e2 ON pa.Assigned_To = e2.Emp_ID
                LEFT JOIN employee e3 ON pa.Updated_By = e3.Emp_ID
                WHERE pa.ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapReaderToModel(reader) : null;
        }

        public async Task<int> CreateAsync(ProjectActivity model)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = @"
                INSERT INTO project_activities (
                    Platform_ID, Solution_ID, Description, Created_By, Created_Time,
                    Assigned_To, Target_Date, Status, Updated_By, Updated_Date
                ) VALUES (
                    @PlatformId, @SolutionId, @Description, @CreatedBy, @CreatedTime,
                    @AssignedTo, @TargetDate, @Status, @UpdatedBy, @UpdatedDate
                );
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, model);
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public async Task<bool> UpdateAsync(ProjectActivity model)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = @"
                UPDATE project_activities SET
                    Platform_ID = @PlatformId, Solution_ID = @SolutionId, Description = @Description,
                    Created_By = @CreatedBy, Created_Time = @CreatedTime, Assigned_To = @AssignedTo,
                    Target_Date = @TargetDate, Status = @Status, Updated_By = @UpdatedBy, UPDATED_Date = @UpdatedDate
                WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", model.Id);
            AddParameters(command, model);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task AddCommentAsync(int activityId, string comment, int? updatedBy)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO project_comments (Activity_ID, Comment, Updated_By, Updated_Time) 
                VALUES (@ActivityId, @Comment, @UpdatedBy, NOW())";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ActivityId", activityId);
            command.Parameters.AddWithValue("@Comment", comment);
            command.Parameters.AddWithValue("@UpdatedBy", updatedBy ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "DELETE FROM project_activities WHERE ID = @Id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT COUNT(1) FROM project_activities WHERE ID = @Id";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        // --- DROPDOWN HELPERS ---

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT Emp_ID, Emp_Name FROM employee ORDER BY Emp_Name";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new Employee
                {
                    EmpId = reader.GetInt32(reader.GetOrdinal("Emp_ID")),
                    EmpName = reader.GetString(reader.GetOrdinal("Emp_Name"))
                });
            }
            return list;
        }

        public async Task<List<MainPlatform>> GetMainPlatformsAsync()
        {
            var list = new List<MainPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT ID, Platforms AS PlatformName FROM main_platforms ORDER BY Platforms";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new MainPlatform
                {
                    ID = reader.GetInt32(reader.GetOrdinal("ID")),
                    Platforms = reader.GetString(reader.GetOrdinal("PlatformName"))
                });
            }
            return list;
        }

        public async Task<List<InternalPlatform>> GetInternalSolutionsAsync()
        {
            var list = new List<InternalPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT ID, App_Name FROM internal_platforms ORDER BY App_Name";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new InternalPlatform
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    AppName = reader.GetString(reader.GetOrdinal("App_Name"))
                });
            }
            return list;
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT * FROM employee WHERE Emp_Email = @Email LIMIT 1";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Employee
                {
                    EmpId = reader.GetInt32(reader.GetOrdinal("Emp_ID")),
                    EmpName = reader.GetString(reader.GetOrdinal("Emp_Name")),
                    EmpEmail = reader.GetString(reader.GetOrdinal("Emp_Email"))
                };
            }
            return null;
        }

        // --- MAPPERS & HELPERS ---

        private ProjectActivity MapReaderToModel(IDataReader reader)
        {
            return new ProjectActivity
            {
                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                PlatformId = reader.GetInt32(reader.GetOrdinal("Platform_ID")),
                SolutionId = GetNullableInt(reader, "Solution_ID"),
                Description = GetNullableString(reader, "Description"),
                CreatedBy = GetNullableInt(reader, "Created_By"),
                CreatedTime = GetNullableDateTime(reader, "Created_Time"),
                AssignedTo = GetNullableInt(reader, "Assigned_To"),
                TargetDate = GetNullableDateTime(reader, "Target_Date"),
                Status = GetNullableString(reader, "Status"),
                UpdatedBy = GetNullableInt(reader, "Updated_By"),
                UpdatedDate = GetNullableDateTime(reader, "Updated_Date"),

                // Joined properties
                PlatformName = TryGetString(reader, "PlatformName"),
                SolutionName = TryGetString(reader, "SolutionName"),
                CreatedByName = TryGetString(reader, "CreatedByName"),
                AssignedToName = TryGetString(reader, "AssignedToName"),
                UpdatedByName = TryGetString(reader, "UpdatedByName"),
                LatestComment = TryGetString(reader, "LatestComment")
            };
        }

        private void AddParameters(MySqlCommand command, ProjectActivity model)
        {
            command.Parameters.AddWithValue("@PlatformId", model.PlatformId);
            command.Parameters.AddWithValue("@SolutionId", model.SolutionId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedTime", model.CreatedTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AssignedTo", model.AssignedTo ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TargetDate", model.TargetDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", model.Status ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UpdatedBy", model.UpdatedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UpdatedDate", model.UpdatedDate ?? (object)DBNull.Value);
        }

        private string? TryGetString(IDataReader reader, string col)
        {
            try { int o = reader.GetOrdinal(col); return reader.IsDBNull(o) ? null : reader.GetString(o); }
            catch { return null; }
        }
        private int? GetNullableInt(IDataReader reader, string col)
        {
            int o = reader.GetOrdinal(col); return reader.IsDBNull(o) ? null : reader.GetInt32(o);
        }
        private string? GetNullableString(IDataReader reader, string col)
        {
            int o = reader.GetOrdinal(col); return reader.IsDBNull(o) ? null : reader.GetString(o);
        }
        private DateTime? GetNullableDateTime(IDataReader reader, string col)
        {
            int o = reader.GetOrdinal(col); return reader.IsDBNull(o) ? null : reader.GetDateTime(o);
        }
    }
}