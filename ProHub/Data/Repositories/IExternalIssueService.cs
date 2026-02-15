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
    public interface IExternalIssueService
    {
        Task<List<ExternalIssue>> GetAllAsync();
        Task<List<ExternalIssue>> SearchAsync(string searchTerm);
        Task<ExternalIssue?> GetByIdAsync(int id);
        Task<int> CreateAsync(ExternalIssue externalIssue);
        Task<bool> UpdateAsync(ExternalIssue externalIssue);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Methods for Dropdowns
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<ExternalPlatform>> GetExternalPlatformsAsync();
        Task<List<CustomerContact>> GetCustomerContactsAsync();
    }

    public class ExternalIssueDataAccess : IExternalIssueService
    {
        private readonly string _connectionString;

        public ExternalIssueDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        public async Task<List<ExternalIssue>> GetAllAsync()
        {
            var externalIssues = new List<ExternalIssue>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    ei.*,
                    ep.Platform_Name AS PlatformName,
                    cc.Contact_Name AS ReportedByName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM External_Issues ei
                LEFT JOIN external_platforms ep ON ei.Platform_ID = ep.ID
                LEFT JOIN CustomerContacts cc ON ei.Reported_By = cc.ID
                LEFT JOIN employee emp_entered ON ei.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ei.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ei.Assigned_By = emp_assigned_by.Emp_ID
                ORDER BY ei.Issue_Start_Time DESC";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                externalIssues.Add(MapReaderToExternalIssue(reader));
            }
            return externalIssues;
        }

        public async Task<List<ExternalIssue>> SearchAsync(string searchTerm)
        {
            var externalIssues = new List<ExternalIssue>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            // --- THIS QUERY IS NOW FIXED ---
            const string query = @"
                SELECT
                    ei.*,
                    ep.Platform_Name AS PlatformName,
                    cc.Contact_Name AS ReportedByName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM External_Issues ei
                LEFT JOIN external_platforms ep ON ei.Platform_ID = ep.ID
                LEFT JOIN CustomerContacts cc ON ei.Reported_By = cc.ID
                LEFT JOIN employee emp_entered ON ei.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ei.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ei.Assigned_By = emp_assigned_by.Emp_ID
                WHERE ei.Description LIKE @SearchTerm
                   OR ep.Platform_Name LIKE @SearchTerm
                   OR emp_assigned_to.Emp_Name LIKE @SearchTerm
                   OR ei.Status LIKE @SearchTerm
                   OR cc.Contact_Name LIKE @SearchTerm
                   OR emp_entered.Emp_Name LIKE @SearchTerm
                   OR CONCAT('E', LPAD(ei.ID, 6, '0')) LIKE @SearchTerm
                ORDER BY ei.Issue_Start_Time DESC";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                externalIssues.Add(MapReaderToExternalIssue(reader));
            }
            return externalIssues;
        }

        public async Task<ExternalIssue?> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    ei.*,
                    ep.Platform_Name AS PlatformName,
                    cc.Contact_Name AS ReportedByName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM External_Issues ei
                LEFT JOIN external_platforms ep ON ei.Platform_ID = ep.ID
                LEFT JOIN CustomerContacts cc ON ei.Reported_By = cc.ID
                LEFT JOIN employee emp_entered ON ei.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ei.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ei.Assigned_By = emp_assigned_by.Emp_ID
                WHERE ei.ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToExternalIssue(reader);
            }
            return null;
        }

        public async Task<int> CreateAsync(ExternalIssue externalIssue)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO External_Issues (
                    Issue_Start_Time, Platform_ID, Reported_By, Description, Criticality,
                    Entered_By, Assigned_To, Assigned_By, Assigned_Time, Status,
                    Issue_Closed_Time, Action_Taken, Entered_Time
                ) VALUES (
                    @IssueStartTime, @PlatformId, @ReportedBy, @Description, @Criticality,
                    @EnteredBy, @AssignedTo, @AssignedBy, @AssignedTime, @Status,
                    @IssueClosedTime, @ActionTaken, @EnteredTime
                ); SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, externalIssue);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
        }

        public async Task<bool> UpdateAsync(ExternalIssue externalIssue)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                UPDATE External_Issues SET
                    Issue_Start_Time = @IssueStartTime,
                    Platform_ID = @PlatformId,
                    Reported_By = @ReportedBy,
                    Description = @Description,
                    Criticality = @Criticality,
                    Entered_By = @EnteredBy,
                    Assigned_To = @AssignedTo,
                    Assigned_By = @AssignedBy,
                    Assigned_Time = @AssignedTime,
                    Status = @Status,
                    Issue_Closed_Time = @IssueClosedTime,
                    Action_Taken = @ActionTaken,
                    Entered_Time = @EnteredTime
                WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", externalIssue.Id);
            AddParameters(command, externalIssue);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "DELETE FROM External_Issues WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT COUNT(1) FROM External_Issues WHERE ID = @Id";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        // Dropdown Data Methods
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT e.Emp_ID, e.Emp_Name
                FROM employee e
                LEFT JOIN empgroup g ON e.GroupID = g.GroupID
                WHERE g.GroupName IS NULL
                   OR g.GroupName <> 'Inactive'
                ORDER BY e.Emp_Name";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Employee
                {
                    EmpId = reader.GetInt32("Emp_ID"),
                    EmpName = reader.GetString("Emp_Name")
                });
            }

            return list;
        }


        public async Task<List<ExternalPlatform>> GetExternalPlatformsAsync()
        {
            var list = new List<ExternalPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = "SELECT ID, Platform_Name FROM external_platforms ORDER BY Platform_Name";
            using var command = new MySqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ExternalPlatform
                {
                    Id = reader.GetInt32("ID"),
                    PlatformName = reader.GetString("Platform_Name")
                });
            }
            return list;
        }

        public async Task<List<CustomerContact>> GetCustomerContactsAsync()
        {
            var list = new List<CustomerContact>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = "SELECT ID, Contact_Name FROM CustomerContacts ORDER BY Contact_Name";
            using var command = new MySqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new CustomerContact
                {
                    ID = reader.GetInt32("ID"),
                    Contact_Name = GetNullableString(reader, "Contact_Name")
                });
            }
            return list;
        }

        // Helper Methods
        private ExternalIssue MapReaderToExternalIssue(IDataReader reader)
        {
            return new ExternalIssue
            {
                Id = GetInt32(reader, "ID"),
                IssueStartTime = GetNullableDateTime(reader, "Issue_Start_Time"),
                PlatformId = GetInt32(reader, "Platform_ID"),
                ReportedBy = GetNullableInt32(reader, "Reported_By"),
                Description = GetNullableString(reader, "Description"),
                Criticality = GetNullableString(reader, "Criticality"),
                EnteredBy = GetNullableInt32(reader, "Entered_By"),
                AssignedTo = GetNullableInt32(reader, "Assigned_To"),
                AssignedBy = GetNullableInt32(reader, "Assigned_By"),
                AssignedTime = GetNullableDateTime(reader, "Assigned_Time"),
                Status = GetNullableString(reader, "Status"),
                IssueClosedTime = GetNullableDateTime(reader, "Issue_Closed_Time"),
                ActionTaken = GetNullableString(reader, "Action_Taken"),
                SLADuration = GetNullableInt32(reader, "SLA_Duration"),
                SLAachieved = GetNullableBool(reader, "SLAachieved"),
                EnteredTime = GetNullableDateTime(reader, "Entered_Time"),

                // Joined properties
                PlatformName = GetNullableString(reader, "PlatformName"),
                ReportedByName = GetNullableString(reader, "ReportedByName"),
                EnteredByName = GetNullableString(reader, "EnteredByName"),
                AssignedToName = GetNullableString(reader, "AssignedToName"),
                AssignedByName = GetNullableString(reader, "AssignedByName")
            };
        }

        private void AddParameters(MySqlCommand command, ExternalIssue externalIssue)
        {
            command.Parameters.AddWithValue("@IssueStartTime", (object)externalIssue.IssueStartTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@PlatformId", externalIssue.PlatformId);
            command.Parameters.AddWithValue("@ReportedBy", (object)externalIssue.ReportedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object)externalIssue.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Criticality", (object)externalIssue.Criticality ?? DBNull.Value);
            command.Parameters.AddWithValue("@EnteredBy", (object)externalIssue.EnteredBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@AssignedTo", (object)externalIssue.AssignedTo ?? DBNull.Value);
            command.Parameters.AddWithValue("@AssignedBy", (object)externalIssue.AssignedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@AssignedTime", (object)externalIssue.AssignedTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object)externalIssue.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@IssueClosedTime", (object)externalIssue.IssueClosedTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@ActionTaken", (object)externalIssue.ActionTaken ?? DBNull.Value);
            command.Parameters.AddWithValue("@EnteredTime", (object)externalIssue.EnteredTime ?? DBNull.Value);
        }

        // Null-safe reader methods
        private static int GetInt32(IDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
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

        private static string? GetNullableString(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
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

        private static bool? GetNullableBool(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return null;

                var value = reader.GetValue(ordinal);
                return Convert.ToBoolean(value);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}