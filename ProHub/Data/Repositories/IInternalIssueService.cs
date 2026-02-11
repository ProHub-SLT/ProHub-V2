﻿﻿﻿using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using PROHUB.Models;
using ProHub.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace PROHUB.Data
{
    // -----------------------------------------------------------------
    // INTERFACE
    // -----------------------------------------------------------------
    public interface IInternalIssueService
    {
        Task<List<InternalIssue>> GetAllAsync();
        Task<List<InternalIssue>> SearchAsync(string searchTerm);
        Task<InternalIssue?> GetByIdAsync(int id);
        Task<int> CreateAsync(InternalIssue internalIssue);
        Task<bool> UpdateAsync(InternalIssue internalIssue);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        // Methods for Dropdowns
        Task<List<Employee>> GetEmployeesAsync();
        Task<List<InternalPlatform>> GetInternalPlatformsAsync();
    }

    // -----------------------------------------------------------------
    // DATA ACCESS CLASS
    // -----------------------------------------------------------------
    public class InternalIssueDataAccess : IInternalIssueService
    {
        private readonly string _connectionString;
        private readonly ProHub.Data.EmployeeRepository _empRepo;

        public InternalIssueDataAccess(IConfiguration configuration, ProHub.Data.EmployeeRepository empRepo)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            _empRepo = empRepo;
        }

        private MySqlConnection GetConnection() => new MySqlConnection(_connectionString);

        public async Task<List<InternalIssue>> GetAllAsync()
        {
            var internalIssues = new List<InternalIssue>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    ii.*,
                    ip.App_Name AS InternalAppName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM Internal_Issues ii
                LEFT JOIN Internal_Platforms ip ON ii.Internal_APP = ip.ID
                LEFT JOIN employee emp_entered ON ii.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ii.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ii.Assigned_By = emp_assigned_by.Emp_ID
                ORDER BY ii.Issue_Start_Time DESC;
            ";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                internalIssues.Add(MapReaderToInternalIssue(reader));
            }

            return internalIssues;
        }

        public async Task<List<InternalIssue>> SearchAsync(string searchTerm)
        {
            var internalIssues = new List<InternalIssue>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    ii.*,
                    ip.App_Name AS InternalAppName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM Internal_Issues ii
                LEFT JOIN Internal_Platforms ip ON ii.Internal_APP = ip.ID
                LEFT JOIN employee emp_entered ON ii.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ii.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ii.Assigned_By = emp_assigned_by.Emp_ID
                WHERE ii.Description LIKE @SearchTerm
                   OR ip.App_Name LIKE @SearchTerm
                   OR emp_assigned_to.Emp_Name LIKE @SearchTerm
                   OR ii.Status LIKE @SearchTerm
                   OR ii.Reported_By LIKE @SearchTerm
                   OR emp_entered.Emp_Name LIKE @SearchTerm
                   OR CONCAT('I', LPAD(ii.ID, 6, '0')) LIKE @SearchTerm
                ORDER BY ii.Issue_Start_Time DESC;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                internalIssues.Add(MapReaderToInternalIssue(reader));
            }

            return internalIssues;
        }

        public async Task<InternalIssue?> GetByIdAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT
                    ii.*,
                    ip.App_Name AS InternalAppName,
                    emp_entered.Emp_Name AS EnteredByName,
                    emp_assigned_to.Emp_Name AS AssignedToName,
                    emp_assigned_by.Emp_Name AS AssignedByName
                FROM Internal_Issues ii
                LEFT JOIN Internal_Platforms ip ON ii.Internal_APP = ip.ID
                LEFT JOIN employee emp_entered ON ii.Entered_By = emp_entered.Emp_ID
                LEFT JOIN employee emp_assigned_to ON ii.Assigned_To = emp_assigned_to.Emp_ID
                LEFT JOIN employee emp_assigned_by ON ii.Assigned_By = emp_assigned_by.Emp_ID
                WHERE ii.ID = @Id;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToInternalIssue(reader);
            }

            return null;
        }

        public async Task<int> CreateAsync(InternalIssue internalIssue)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO Internal_Issues (
                    Issue_Start_Time, Internal_APP, Reported_By, Description, Criticality,
                    Entered_By, Assigned_To, Assigned_By, Assigned_Time, Status,
                    Issue_Closed_Time, Action_Taken, Entered_Time, Reporting_Person_ContactNo
                ) VALUES (
                    @IssueStartTime, @InternalAppId, @ReportedBy, @Description, @Criticality,
                    @EnteredBy, @AssignedTo, @AssignedBy, @AssignedTime, @Status,
                    @IssueClosedTime, @ActionTaken, @EnteredTime, @ReportingPersonContactNo
                );
                SELECT LAST_INSERT_ID();
            ";

            using var command = new MySqlCommand(query, connection);
            AddParameters(command, internalIssue);
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
        }

        public async Task<bool> UpdateAsync(InternalIssue internalIssue)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                UPDATE Internal_Issues SET
                    Issue_Start_Time = @IssueStartTime,
                    Internal_APP = @InternalAppId,
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
                    Entered_Time = @EnteredTime,
                    Reporting_Person_ContactNo = @ReportingPersonContactNo
                WHERE ID = @Id;
            ";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", internalIssue.Id);
            AddParameters(command, internalIssue);
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                const string query = "DELETE FROM Internal_Issues WHERE ID = @Id;";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (MySqlException ex)
            {
                // 1451 = foreign key constraint fails (cannot delete)
                if (ex.Number == 1451)
                {
                    Console.WriteLine($"Failed to delete issue {id} due to foreign key constraint: {ex.Message}");
                    return false;
                }
                Console.WriteLine($"SQL error during delete: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error during delete: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = "SELECT COUNT(1) FROM Internal_Issues WHERE ID = @Id;";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
        }

        // DROPDOWN DATA METHODS
        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var list = new List<Employee>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = @"
                SELECT e.Emp_ID, e.Emp_Name
                FROM employee e
                LEFT JOIN EmpGroup g ON e.GroupID = g.GroupID
                WHERE g.GroupName IS NULL
                   OR g.GroupName <> 'Inactive'
                ORDER BY e.Emp_Name;
            ";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Employee
                {
                    EmpId = reader.IsDBNull(reader.GetOrdinal("Emp_ID")) ? 0 : reader.GetInt32("Emp_ID"),
                    EmpName = reader.IsDBNull(reader.GetOrdinal("Emp_Name")) ? null : reader.GetString("Emp_Name")
                });
            }

            return list;
        }


        public async Task<List<InternalPlatform>> GetInternalPlatformsAsync()
        {
            var list = new List<InternalPlatform>();
            using var connection = GetConnection();
            await connection.OpenAsync();

            const string query = "SELECT ID, App_Name FROM Internal_Platforms ORDER BY App_Name;";
            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new InternalPlatform
                {
                    Id = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32("ID"),
                    AppName = reader.IsDBNull(reader.GetOrdinal("App_Name")) ? null : reader.GetString("App_Name")
                });
            }

            return list;
        }

        // HELPER METHODS
        private InternalIssue MapReaderToInternalIssue(IDataReader reader)
        {
            return new InternalIssue
            {
                Id = GetInt32(reader, "ID"),
                IssueStartTime = GetNullableDateTime(reader, "Issue_Start_Time"),
                InternalAppId = GetInt32(reader, "Internal_APP"),
                ReportedBy = GetNullableString(reader, "Reported_By"),
                ReportingPersonContactNo = GetNullableString(reader, "Reporting_Person_ContactNo"),
                Description = GetNullableString(reader, "Description"),
                Criticality = GetNullableString(reader, "Criticality"),
                EnteredBy = GetNullableInt32(reader, "Entered_By"),
                AssignedTo = GetNullableInt32(reader, "Assigned_To"),
                AssignedBy = GetNullableInt32(reader, "Assigned_By"),
                AssignedTime = GetNullableDateTime(reader, "Assigned_Time"),
                Status = GetNullableString(reader, "Status"),
                IssueClosedTime = GetNullableDateTime(reader, "Issue_Closed_Time"),
                ActionTaken = GetNullableString(reader, "Action_Taken"),
                EnteredTime = GetNullableDateTime(reader, "Entered_Time"),
                // Joined properties (aliased in SQL)
                InternalAppName = GetNullableString(reader, "InternalAppName"),
                EnteredByName = GetNullableString(reader, "EnteredByName"),
                AssignedToName = GetNullableString(reader, "AssignedToName"),
                AssignedByName = GetNullableString(reader, "AssignedByName")
            };
        }

        private void AddParameters(MySqlCommand command, InternalIssue internalIssue)
        {
            command.Parameters.AddWithValue("@IssueStartTime", (object?)internalIssue.IssueStartTime ?? DBNull.Value);

            if (internalIssue.InternalAppId == null || internalIssue.InternalAppId == 0)
                command.Parameters.AddWithValue("@InternalAppId", DBNull.Value);
            else
                command.Parameters.AddWithValue("@InternalAppId", internalIssue.InternalAppId);

            command.Parameters.AddWithValue("@ReportedBy", (object?)internalIssue.ReportedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)internalIssue.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Criticality", (object?)internalIssue.Criticality ?? DBNull.Value);

            if (internalIssue.EnteredBy == null || internalIssue.EnteredBy == 0)
                command.Parameters.AddWithValue("@EnteredBy", DBNull.Value);
            else
                command.Parameters.AddWithValue("@EnteredBy", internalIssue.EnteredBy);

            if (internalIssue.AssignedTo == null || internalIssue.AssignedTo == 0)
                command.Parameters.AddWithValue("@AssignedTo", DBNull.Value);
            else
                command.Parameters.AddWithValue("@AssignedTo", internalIssue.AssignedTo);

            if (internalIssue.AssignedBy == null || internalIssue.AssignedBy == 0)
                command.Parameters.AddWithValue("@AssignedBy", DBNull.Value);
            else
                command.Parameters.AddWithValue("@AssignedBy", internalIssue.AssignedBy);

            command.Parameters.AddWithValue("@AssignedTime", (object?)internalIssue.AssignedTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object?)internalIssue.Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@IssueClosedTime", (object?)internalIssue.IssueClosedTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@ActionTaken", (object?)internalIssue.ActionTaken ?? DBNull.Value);
            command.Parameters.AddWithValue("@EnteredTime", (object?)internalIssue.EnteredTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReportingPersonContactNo", (object?)internalIssue.ReportingPersonContactNo ?? DBNull.Value);
        }

        // NULL-SAFE READER METHODS
        private static int GetInt32(IDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
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
    }
}
