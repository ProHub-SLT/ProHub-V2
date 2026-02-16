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
                        e.emp_name AS SupervisorName, 
                        f.field_of_spec_name AS FieldOfSpecName 
                    FROM 
                        trainee t
                    LEFT JOIN 
                        employee e ON t.supervisor = e.emp_id
                    LEFT JOIN 
                        fields_of_specialization f ON t.field_of_spec_id = f.field_of_spec_id
                    ORDER BY 
                        t.trainee_id DESC";

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
                        e.emp_name AS SupervisorName, 
                        f.field_of_spec_name AS FieldOfSpecName 
                    FROM 
                        trainee t
                    LEFT JOIN 
                        employee e ON t.supervisor = e.emp_id
                    LEFT JOIN 
                        fields_of_specialization f ON t.field_of_spec_id = f.field_of_spec_id
                    WHERE 
                        t.trainee_id = @ID";

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
                    INSERT INTO trainee
                    (trainee_name, trainee_phone, trainee_nic, trainee_email,
                     training_startdate, training_enddate, institute, languages_known,
                     supervisor, target_date, trainee_homeaddress, assignedwork_desc,
                     field_of_spec_id, payment_start_date, payment_end_date,
                     requested_payment_date, absent_count, terminated_date, terminated_reason)
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
                    UPDATE trainee SET
                        trainee_name = @Trainee_Name,
                        trainee_phone = @Trainee_Phone,
                        trainee_nic = @Trainee_NIC,
                        trainee_email = @Trainee_Email,
                        trainee_homeaddress = @Trainee_HomeAddress,
                        training_startdate = @Training_StartDate,
                        training_enddate = @Training_EndDate,
                        institute = @Institute,
                        languages_known = @Languages_Known,
                        supervisor = @Supervisor,
                        target_date = @Target_Date,
                        assignedwork_desc = @AssignedWork_Desc,
                        field_of_spec_id = @field_of_spec_id,
                        payment_start_date = @payment_start_date,
                        payment_end_date = @payment_end_date,
                        requested_payment_date = @requested_payment_date,
                        absent_count = @absent_Count,
                        terminated_date = @terminated_date,
                        terminated_reason = @terminated_reason
                    WHERE trainee_id = @Trainee_ID";

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

                string query = "DELETE FROM trainee WHERE trainee_id = @ID";
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
                Trainee_ID = reader.GetInt32("trainee_id"),
                Trainee_Name = reader.GetString("trainee_name"),

                Trainee_Phone = reader.IsDBNull("trainee_phone") ? null : reader.GetString("trainee_phone"),
                Trainee_NIC = reader.IsDBNull("trainee_nic") ? null : reader.GetString("trainee_nic"),
                Trainee_Email = reader.IsDBNull("trainee_email") ? null : reader.GetString("trainee_email"),
                Trainee_HomeAddress = reader.IsDBNull("trainee_homeaddress") ? null : reader.GetString("trainee_homeaddress"),

                Training_StartDate = reader.IsDBNull("training_startdate") ? null : reader.GetDateTime("training_startdate"),
                Training_EndDate = reader.IsDBNull("training_enddate") ? null : reader.GetDateTime("training_enddate"),

                Institute = reader.IsDBNull("institute") ? null : reader.GetString("institute"),
                Languages_Known = reader.IsDBNull("languages_known") ? null : reader.GetString("languages_known"),

                Supervisor = reader.IsDBNull("supervisor") ? null : reader.GetInt32("supervisor"),
                field_of_spec_id = reader.IsDBNull("field_of_spec_id") ? null : reader.GetInt32("field_of_spec_id"),

                Target_Date = reader.IsDBNull("target_date") ? null : reader.GetDateTime("target_date"),

                AssignedWork_Desc = reader.IsDBNull("assignedwork_desc") ? null : reader.GetString("assignedwork_desc"),

                payment_start_date = reader.IsDBNull("payment_start_date") ? null : reader.GetDateTime("payment_start_date"),
                payment_end_date = reader.IsDBNull("payment_end_date") ? null : reader.GetDateTime("payment_end_date"),

                // This matches the new column name in your database
                requested_payment_date = reader.IsDBNull("requested_payment_date") ? null : reader.GetDateTime("requested_payment_date"),

                absent_Count = reader.IsDBNull("absent_count") ? 0 : reader.GetInt32("absent_count"),

                terminated_date = reader.IsDBNull("terminated_date") ? null : reader.GetDateTime("terminated_date"),
                terminated_reason = reader.IsDBNull("terminated_reason") ? null : reader.GetString("terminated_reason"),

                SupervisorName = reader.IsDBNull("SupervisorName") ? null : reader.GetString("SupervisorName"),
                FieldOfSpecName = reader.IsDBNull("FieldOfSpecName") ? null : reader.GetString("FieldOfSpecName")
            };
        }
    }
}