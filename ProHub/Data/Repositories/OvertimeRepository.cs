// File: Data/OvertimeRepository.cs
using MySql.Data.MySqlClient;
using ProHub.Models;
using System;
using System.Collections.Generic;

namespace ProHub.Data
{
    public class OvertimeRepository
    {
        private readonly IConfiguration _configuration;
        private readonly EmployeeRepository _empRepo;

        public OvertimeRepository(IConfiguration configuration, EmployeeRepository empRepo)
        {
            _configuration = configuration;
            _empRepo = empRepo;
        }

        private MySqlConnection GetConnection() => new(_configuration.GetConnectionString("DefaultConnection"));

        public List<OvertimeRequest> GetAll(string search = "", int page = 1, int pageSize = 10)
        {
            var list = new List<OvertimeRequest>();
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                SELECT o.*, 
                       c.Emp_Name AS CreatedByName,
                       a.Emp_Name AS ApprovalForName,
                       ap.Emp_Name AS ApprovedByName
                FROM OverTime_Data o
                LEFT JOIN Employee c ON o.Created_By = c.Emp_ID
                LEFT JOIN Employee a ON o.Approval_For = a.Emp_ID
                LEFT JOIN Employee ap ON o.Approved_By = ap.Emp_ID
                WHERE (o.Date LIKE @search OR c.Emp_Name LIKE @search OR a.Emp_Name LIKE @search)
                ORDER BY o.Created_Date DESC
                LIMIT @offset, @pageSize";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new OvertimeRequest
                {
                    ID = r.GetInt32("ID"),
                    Created_Date = r.IsDBNull(r.GetOrdinal("Created_Date")) ? null : r.GetDateTime("Created_Date"),
                    Created_By = r.IsDBNull(r.GetOrdinal("Created_By")) ? null : r.GetInt32("Created_By"),
                    Date = r.IsDBNull(r.GetOrdinal("Date")) ? null : r.GetDateTime("Date"),
                    No_Of_Hours = r.IsDBNull(r.GetOrdinal("No_Of_Hours")) ? null : r.GetDecimal("No_Of_Hours"),
                    Work_Description = r.IsDBNull(r.GetOrdinal("Work_Description")) ? null : r.GetString("Work_Description"),
                    Approval_For = r.IsDBNull(r.GetOrdinal("Approval_For")) ? null : r.GetInt32("Approval_For"),
                    Comment = r.IsDBNull(r.GetOrdinal("Comment")) ? null : r.GetString("Comment"),
                    Approved_By = r.IsDBNull(r.GetOrdinal("Approved_By")) ? null : r.GetInt32("Approved_By"),
                    Approved_Date = r.IsDBNull(r.GetOrdinal("Approved_Date")) ? null : r.GetDateTime("Approved_Date"),
                    CreatedByName = r.IsDBNull(r.GetOrdinal("CreatedByName")) ? "Unknown" : r.GetString("CreatedByName"),
                    ApprovalForName = r.IsDBNull(r.GetOrdinal("ApprovalForName")) ? "None" : r.GetString("ApprovalForName"),
                    ApprovedByName = r.IsDBNull(r.GetOrdinal("ApprovedByName")) ? "Pending" : r.GetString("ApprovedByName")
                });
            }
            return list;
        }

        public int GetCount(string search = "")
        {
            using var conn = GetConnection();
            conn.Open();
            string sql = "SELECT COUNT(*) FROM OverTime_Data o " +
                         "LEFT JOIN Employee c ON o.Created_By = c.Emp_ID " +
                         "WHERE o.Date LIKE @search OR c.Emp_Name LIKE @search";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Insert(OvertimeRequest ot)
        {
            using var conn = GetConnection();
            conn.Open();
            string sql = @"
                INSERT INTO OverTime_Data 
                (Created_Date, Created_By, Date, No_Of_Hours, Work_Description, Approval_For)
                VALUES (NOW(), @Created_By, @Date, @No_Of_Hours, @Work_Description, @Approval_For)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Created_By", ot.Created_By ?? 5);
            cmd.Parameters.AddWithValue("@Date", ot.Date ?? DateTime.Today);
            cmd.Parameters.AddWithValue("@No_Of_Hours", ot.No_Of_Hours ?? 0);
            cmd.Parameters.AddWithValue("@Work_Description", ot.Work_Description ?? "");
            cmd.Parameters.AddWithValue("@Approval_For", ot.Approval_For ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public OvertimeRequest GetById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                SELECT o.*, 
                       c.Emp_Name AS CreatedByName,
                       a.Emp_Name AS ApprovalForName,
                       ap.Emp_Name AS ApprovedByName
                FROM OverTime_Data o
                LEFT JOIN Employee c ON o.Created_By = c.Emp_ID
                LEFT JOIN Employee a ON o.Approval_For = a.Emp_ID
                LEFT JOIN Employee ap ON o.Approved_By = ap.Emp_ID
                WHERE o.ID = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return new OvertimeRequest
                {
                    ID = r.GetInt32("ID"),
                    Created_Date = r.IsDBNull(r.GetOrdinal("Created_Date")) ? null : r.GetDateTime("Created_Date"),
                    Created_By = r.IsDBNull(r.GetOrdinal("Created_By")) ? null : r.GetInt32("Created_By"),
                    Date = r.IsDBNull(r.GetOrdinal("Date")) ? null : r.GetDateTime("Date"),
                    No_Of_Hours = r.IsDBNull(r.GetOrdinal("No_Of_Hours")) ? null : r.GetDecimal("No_Of_Hours"),
                    Work_Description = r.IsDBNull(r.GetOrdinal("Work_Description")) ? null : r.GetString("Work_Description"),
                    Approval_For = r.IsDBNull(r.GetOrdinal("Approval_For")) ? null : r.GetInt32("Approval_For"),
                    Comment = r.IsDBNull(r.GetOrdinal("Comment")) ? null : r.GetString("Comment"),
                    Approved_By = r.IsDBNull(r.GetOrdinal("Approved_By")) ? null : r.GetInt32("Approved_By"),
                    Approved_Date = r.IsDBNull(r.GetOrdinal("Approved_Date")) ? null : r.GetDateTime("Approved_Date"),
                    CreatedByName = r.IsDBNull(r.GetOrdinal("CreatedByName")) ? "Unknown" : r.GetString("CreatedByName"),
                    ApprovalForName = r.IsDBNull(r.GetOrdinal("ApprovalForName")) ? "None" : r.GetString("ApprovalForName"),
                    ApprovedByName = r.IsDBNull(r.GetOrdinal("ApprovedByName")) ? "Pending" : r.GetString("ApprovedByName")
                };
            }

            return null;
        }

        public void Update(OvertimeRequest ot)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                UPDATE OverTime_Data 
                SET Date = @Date, 
                    No_Of_Hours = @No_Of_Hours, 
                    Work_Description = @Work_Description, 
                    Approval_For = @Approval_For,
                    Comment = @Comment
                WHERE ID = @ID";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ID", ot.ID);
            cmd.Parameters.AddWithValue("@Date", ot.Date ?? DateTime.Today);
            cmd.Parameters.AddWithValue("@No_Of_Hours", ot.No_Of_Hours ?? 0);
            cmd.Parameters.AddWithValue("@Work_Description", ot.Work_Description ?? "");
            cmd.Parameters.AddWithValue("@Approval_For", ot.Approval_For ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Comment", ot.Comment ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = "DELETE FROM OverTime_Data WHERE ID = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}