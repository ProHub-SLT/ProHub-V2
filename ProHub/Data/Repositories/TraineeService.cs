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
    public interface ITraineeService
    {
        Task<List<Trainees>> GetAllAsync();
        Task<Trainees?> GetByIdAsync(int id);
        Task<int> CreateAsync(Trainees trainee);
        Task<bool> UpdateAsync(Trainees trainee);
        Task<bool> DeleteAsync(int id);
    }

    public class TraineeDataAccess : ITraineeService
    {
        private readonly string? _connectionString;

        public TraineeDataAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<List<Trainees>> GetAllAsync()
        {
            var trainees = new List<Trainees>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        t.*, 
                        e.Emp_Name AS SupervisorName, 
                        f.field_of_spec_name AS FieldOfSpecName 
                    FROM 
                        Trainee t
                    LEFT JOIN 
                        Employee e ON t.Supervisor = e.Emp_ID
                    LEFT JOIN 
                        Fields_of_Specialization f ON t.field_of_spec_id = f.field_of_spec_id
                    ORDER BY 
                        t.Trainee_ID DESC";

                using (var cmd = new MySqlCommand(query, connection))
                using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        trainees.Add(MapReaderToTrainee(reader));
                    }
                }
            }

            return trainees;
        }

        public async Task<Trainees?> GetByIdAsync(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT 
                        t.*, 
                        e.Emp_Name AS SupervisorName, 
                        f.field_of_spec_name AS FieldOfSpecName 
                    FROM 
                        Trainee t
                    LEFT JOIN 
                        Employee e ON t.Supervisor = e.Emp_ID
                    LEFT JOIN 
                        Fields_of_Specialization f ON t.field_of_spec_id = f.field_of_spec_id
                    WHERE 
                        t.Trainee_ID = @ID";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);

                    using (MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return MapReaderToTrainee(reader);
                        }
                    }
                }
            }

            return null;
        }

        public async Task<int> CreateAsync(Trainees trainee)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // FIX: Changed requested_payment_amount to requested_payment_date
                string query = @"
                    INSERT INTO Trainee
                    (Trainee_Name, Trainee_Phone, Trainee_NIC, Trainee_Email,
                     Training_StartDate, Training_EndDate, Institute, Languages_Known,
                     Supervisor, Target_Date, Trainee_HomeAddress, AssignedWork_Desc,
                     field_of_spec_id, payment_start_date, payment_end_date,
                     requested_payment_date, absent_Count, terminated_date, terminated_reason)
                    VALUES
                    (@Trainee_Name, @Trainee_Phone, @Trainee_NIC, @Trainee_Email,
                     @Training_StartDate, @Training_EndDate, @Institute, @Languages_Known,
                     @Supervisor, @Target_Date, @Trainee_HomeAddress, @AssignedWork_Desc,
                     @field_of_spec_id, @payment_start_date, @payment_end_date,
                     @requested_payment_date, @absent_Count, @terminated_date, @terminated_reason);
                    SELECT LAST_INSERT_ID();";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddParameters(cmd, trainee);
                    var result = await cmd.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task<bool> UpdateAsync(Trainees trainee)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // FIX: Changed requested_payment_amount to requested_payment_date in the SET clause
                string query = @"
                    UPDATE Trainee SET
                        Trainee_Name = @Trainee_Name,
                        Trainee_Phone = @Trainee_Phone,
                        Trainee_NIC = @Trainee_NIC,
                        Trainee_Email = @Trainee_Email,
                        Trainee_HomeAddress = @Trainee_HomeAddress,
                        Training_StartDate = @Training_StartDate,
                        Training_EndDate = @Training_EndDate,
                        Institute = @Institute,
                        Languages_Known = @Languages_Known,
                        Supervisor = @Supervisor,
                        Target_Date = @Target_Date,
                        AssignedWork_Desc = @AssignedWork_Desc,
                        field_of_spec_id = @field_of_spec_id,
                        payment_start_date = @payment_start_date,
                        payment_end_date = @payment_end_date,
                        requested_payment_date = @requested_payment_date,
                        absent_Count = @absent_Count,
                        terminated_date = @terminated_date,
                        terminated_reason = @terminated_reason
                    WHERE Trainee_ID = @Trainee_ID";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    AddParameters(cmd, trainee);
                    cmd.Parameters.AddWithValue("@Trainee_ID", trainee.Trainee_ID);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "DELETE FROM trainee WHERE Trainee_ID = @ID";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }

        // ⭐ HELPER METHOD: Binds properties to SQL. 
        // FIX: Updated to bind @requested_payment_date
        private void AddParameters(MySqlCommand cmd, Trainees trainee)
        {
            cmd.Parameters.AddWithValue("@Trainee_Name", trainee.Trainee_Name);
            cmd.Parameters.AddWithValue("@Trainee_Phone", trainee.Trainee_Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Trainee_NIC", trainee.Trainee_NIC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Trainee_Email", trainee.Trainee_Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Training_StartDate", trainee.Training_StartDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Training_EndDate", trainee.Training_EndDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Institute", trainee.Institute ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Languages_Known", trainee.Languages_Known ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Supervisor", trainee.Supervisor ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Target_Date", trainee.Target_Date ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Trainee_HomeAddress", trainee.Trainee_HomeAddress ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@AssignedWork_Desc", trainee.AssignedWork_Desc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@field_of_spec_id", trainee.field_of_spec_id ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@payment_start_date", trainee.payment_start_date ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@payment_end_date", trainee.payment_end_date ?? (object)DBNull.Value);

            // This is the key fix for your error:
            cmd.Parameters.AddWithValue("@requested_payment_date", trainee.requested_payment_date ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@absent_Count", trainee.absent_Count);
            cmd.Parameters.AddWithValue("@terminated_date", trainee.terminated_date ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@terminated_reason", trainee.terminated_reason ?? (object)DBNull.Value);
        }

        // ⭐ HELPER METHOD: Maps DB to Model
        // FIX: Updated to read requested_payment_date
        private Trainees MapReaderToTrainee(MySqlDataReader reader)
        {
            return new Trainees
            {
                Trainee_ID = reader.GetInt32("Trainee_ID"),
                Trainee_Name = reader.GetString("Trainee_Name"),

                Trainee_Phone = reader.IsDBNull("Trainee_Phone") ? null : reader.GetString("Trainee_Phone"),
                Trainee_NIC = reader.IsDBNull("Trainee_NIC") ? null : reader.GetString("Trainee_NIC"),
                Trainee_Email = reader.IsDBNull("Trainee_Email") ? null : reader.GetString("Trainee_Email"),
                Trainee_HomeAddress = reader.IsDBNull("Trainee_HomeAddress") ? null : reader.GetString("Trainee_HomeAddress"),

                Training_StartDate = reader.IsDBNull("Training_StartDate") ? null : reader.GetDateTime("Training_StartDate"),
                Training_EndDate = reader.IsDBNull("Training_EndDate") ? null : reader.GetDateTime("Training_EndDate"),

                Institute = reader.IsDBNull("Institute") ? null : reader.GetString("Institute"),
                Languages_Known = reader.IsDBNull("Languages_Known") ? null : reader.GetString("Languages_Known"),

                Supervisor = reader.IsDBNull("Supervisor") ? null : reader.GetInt32("Supervisor"),
                field_of_spec_id = reader.IsDBNull("field_of_spec_id") ? null : reader.GetInt32("field_of_spec_id"),

                Target_Date = reader.IsDBNull("Target_Date") ? null : reader.GetDateTime("Target_Date"),

                AssignedWork_Desc = reader.IsDBNull("AssignedWork_Desc") ? null : reader.GetString("AssignedWork_Desc"),

                payment_start_date = reader.IsDBNull("payment_start_date") ? null : reader.GetDateTime("payment_start_date"),
                payment_end_date = reader.IsDBNull("payment_end_date") ? null : reader.GetDateTime("payment_end_date"),

                // This matches the new column name in your database
                requested_payment_date = reader.IsDBNull("requested_payment_date") ? null : reader.GetDateTime("requested_payment_date"),

                absent_Count = reader.IsDBNull("absent_Count") ? 0 : reader.GetInt32("absent_Count"),

                terminated_date = reader.IsDBNull("terminated_date") ? null : reader.GetDateTime("terminated_date"),
                terminated_reason = reader.IsDBNull("terminated_reason") ? null : reader.GetString("terminated_reason"),

                SupervisorName = reader.IsDBNull("SupervisorName") ? null : reader.GetString("SupervisorName"),
                FieldOfSpecName = reader.IsDBNull("FieldOfSpecName") ? null : reader.GetString("FieldOfSpecName")
            };
        }
    }
}