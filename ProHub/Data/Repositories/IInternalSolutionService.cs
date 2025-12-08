using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using ProHub.Models; // <-- use the namespace where your models live

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
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT
                        isol.*,
                        emp.Emp_Name AS DevelopedByName,
                        sp.Phase AS SDLCPhaseName,
                        te.EndUserType AS EndUserTypeName,
                        parent.App_Name AS MainAppName,
                        pp.ParentProjectGroup AS ParentProjectGroupName
                    FROM Internal_Platforms isol
                    LEFT JOIN Employee emp ON isol.Developed_By = emp.Emp_ID
                    LEFT JOIN SDLCPhas sp ON isol.SDLCPhase = sp.ID
                    LEFT JOIN Targetenduser te ON isol.EndUserType = te.ID
                    LEFT JOIN Internal_Platforms parent ON isol.MainAppID = parent.ID
                    LEFT JOIN ParentProject pp ON isol.ParentProjectID = pp.ParentProjectID;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    solutions.Add(MapReaderToSolution(reader));
                }

                return solutions;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetAllAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving internal solutions. Please contact support.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllAsync");
                throw new Exception("Failed to retrieve internal solutions.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<InternalPlatform?> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            }

            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    SELECT
                        isol.*,
                        emp.Emp_Name AS DevelopedByName,
                        sp.Phase AS SDLCPhaseName,
                        te.EndUserType AS EndUserTypeName,
                        parent.App_Name AS MainAppName,
                        pp.ParentProjectGroup AS ParentProjectGroupName
                    FROM Internal_Platforms isol
                    LEFT JOIN Employee emp ON isol.Developed_By = emp.Emp_ID
                    LEFT JOIN SDLCPhas sp ON isol.SDLCPhase = sp.ID
                    LEFT JOIN Targetenduser te ON isol.EndUserType = te.ID
                    LEFT JOIN Internal_Platforms parent ON isol.MainAppID = parent.ID
                    LEFT JOIN ParentProject pp ON isol.ParentProjectID = pp.ParentProjectID
                    WHERE isol.ID = @ID;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", id);

                using DbDataReader reader = await command.ExecuteReaderAsync();
                return await reader.ReadAsync() ? MapReaderToSolution(reader) : null;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetByIdAsync for ID {Id}. Error Code: {ErrorCode}", id, ex.Number);
                throw new Exception($"Database error while retrieving solution with ID {id}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetByIdAsync for ID {Id}", id);
                throw new Exception($"Failed to retrieve solution with ID {id}.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<int> CreateAsync(InternalPlatform solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    INSERT INTO Internal_Platforms (
                        App_Name, Developed_By, Developed_Team, StartDate, TargetDate,
                        Bit_bucket_repo, SDLCPhase, PercentageDone, Status, StatusDate,
                        Bus_Owner, App_Category, Scope, App_IP, App_URL, App_Users,
                        UATDate, Integrated_Apps, DR, LaunchedDate, VADate, WAF,
                        APP_OP_Owner, App_Business_Owner, Price, EndUserType, RequestNo,
                        ParentProjectID, SLA, MainAppID, SSLCertificateExpDate
                    ) VALUES (
                        @App_Name, @Developed_By, @Developed_Team, @StartDate, @TargetDate,
                        @Bit_bucket_repo, @SDLCPhase, @PercentageDone, @Status, @StatusDate,
                        @Bus_Owner, @App_Category, @Scope, @App_IP, @App_URL, @App_Users,
                        @UATDate, @Integrated_Apps, @DR, @LaunchedDate, @VADate, @WAF,
                        @APP_OP_Owner, @App_Business_Owner, @Price, @EndUserType, @RequestNo,
                        @ParentProjectID, @SLA, @MainAppID, @SSLCertificateExpDate
                    );
                    SELECT LAST_INSERT_ID();";

                using var command = new MySqlCommand(query, connection);
                AddParameters(command, solution);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in CreateAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while creating solution.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateAsync");
                throw new Exception("Failed to create solution.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<bool> UpdateAsync(InternalPlatform solution)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (solution.Id <= 0)
            {
                throw new ArgumentException("Solution ID must be greater than zero.", nameof(solution));
            }

            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = @"
                    UPDATE Internal_Platforms SET
                        App_Name = @App_Name,
                        Developed_By = @Developed_By,
                        Developed_Team = @Developed_Team,
                        StartDate = @StartDate,
                        TargetDate = @TargetDate,
                        Bit_bucket_repo = @Bit_bucket_repo,
                        SDLCPhase = @SDLCPhase,
                        PercentageDone = @PercentageDone,
                        Status = @Status,
                        StatusDate = @StatusDate,
                        Bus_Owner = @Bus_Owner,
                        App_Category = @App_Category,
                        Scope = @Scope,
                        App_IP = @App_IP,
                        App_URL = @App_URL,
                        App_Users = @App_Users,
                        UATDate = @UATDate,
                        Integrated_Apps = @Integrated_Apps,
                        DR = @DR,
                        LaunchedDate = @LaunchedDate,
                        VADate = @VADate,
                        WAF = @WAF,
                        APP_OP_Owner = @APP_OP_Owner,
                        App_Business_Owner = @App_Business_Owner,
                        Price = @Price,
                        EndUserType = @EndUserType,
                        RequestNo = @RequestNo,
                        ParentProjectID = @ParentProjectID,
                        SLA = @SLA,
                        MainAppID = @MainAppID,
                        SSLCertificateExpDate = @SSLCertificateExpDate
                    WHERE ID = @ID;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", solution.Id);
                AddParameters(command, solution);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in UpdateAsync for ID {Id}. Error Code: {ErrorCode}", solution.Id, ex.Number);
                throw new Exception($"Database error while updating solution with ID {solution.Id}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateAsync for ID {Id}", solution.Id);
                throw new Exception($"Failed to update solution with ID {solution.Id}.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("ID must be greater than zero.", nameof(id));
            }

            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "DELETE FROM Internal_Platforms WHERE ID = @ID;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in DeleteAsync for ID {Id}. Error Code: {ErrorCode}", id, ex.Number);
                throw new Exception($"Database error while deleting solution with ID {id}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteAsync for ID {Id}", id);
                throw new Exception($"Failed to delete solution with ID {id}.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            if (id <= 0)
            {
                return false;
            }

            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT COUNT(1) FROM Internal_Platforms WHERE ID = @ID;";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID", id);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in ExistsAsync for ID {Id}. Error Code: {ErrorCode}", id, ex.Number);
                throw new Exception($"Database error while checking if solution with ID {id} exists.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ExistsAsync for ID {Id}", id);
                throw new Exception($"Failed to check if solution with ID {id} exists.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            var employees = new List<Employee>();
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT Emp_ID, Emp_Name FROM Employee ORDER BY Emp_Name;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    employees.Add(new Employee
                    {
                        EmpId = reader.GetInt32(reader.GetOrdinal("Emp_ID")),
                        EmpName = reader.IsDBNull(reader.GetOrdinal("Emp_Name"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Emp_Name"))
                    });
                }
                return employees;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetEmployeesAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving employees.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetEmployeesAsync");
                throw new Exception("Failed to retrieve employees.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<List<SDLCPhase>> GetSdlcPhasesAsync()
        {
            var phases = new List<SDLCPhase>();
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT ID, Phase, OrderSeq FROM SDLCPhas ORDER BY OrderSeq, Phase;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    phases.Add(new SDLCPhase
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("ID")),
                        Phase = reader.IsDBNull(reader.GetOrdinal("Phase"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("Phase")),
                        OrderSeq = reader.IsDBNull(reader.GetOrdinal("OrderSeq"))
                            ? (int?)null
                            : reader.GetInt32(reader.GetOrdinal("OrderSeq"))
                    });
                }
                return phases;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetSdlcPhasesAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving SDLC phases.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetSdlcPhasesAsync");
                throw new Exception("Failed to retrieve SDLC phases.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<List<TargetEndUser>> GetEndUserTypesAsync()
        {
            var types = new List<TargetEndUser>();
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT ID, EndUserType FROM Targetenduser ORDER BY EndUserType;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    types.Add(new TargetEndUser
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        EndUserType = reader.IsDBNull(reader.GetOrdinal("EndUserType"))
                            ? string.Empty
                            : reader.GetString(reader.GetOrdinal("EndUserType"))
                    });
                }
                return types;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetEndUserTypesAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving end user types.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetEndUserTypesAsync");
                throw new Exception("Failed to retrieve end user types.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<List<InternalPlatform>> GetMainApplicationsAsync()
        {
            var list = new List<InternalPlatform>();
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT ID, App_Name FROM Internal_Platforms ORDER BY App_Name;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new InternalPlatform
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("ID")),
                        AppName = reader.IsDBNull(reader.GetOrdinal("App_Name"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("App_Name"))
                    });
                }
                return list;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetMainApplicationsAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving main applications.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetMainApplicationsAsync");
                throw new Exception("Failed to retrieve main applications.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        public async Task<List<ParentProject>> GetParentProjectsAsync()
        {
            var list = new List<ParentProject>();
            MySqlConnection? connection = null;

            try
            {
                connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string query = "SELECT ParentProjectID, ParentProjectGroup FROM ParentProject ORDER BY ParentProjectGroup;";

                using var command = new MySqlCommand(query, connection);
                using DbDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    list.Add(new ParentProject
                    {
                        ParentProjectID = reader.GetInt32(reader.GetOrdinal("ParentProjectID")),
                        ParentProjectGroup = reader.IsDBNull(reader.GetOrdinal("ParentProjectGroup"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("ParentProjectGroup"))
                    });
                }
                return list;
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "MySQL error in GetParentProjectsAsync. Error Code: {ErrorCode}", ex.Number);
                throw new Exception("Database error while retrieving parent projects.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetParentProjectsAsync");
                throw new Exception("Failed to retrieve parent projects.", ex);
            }
            finally
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
        }

        private InternalPlatform MapReaderToSolution(DbDataReader reader)
        {
            string? SafeGetString(string name)
            {
                var ordinal = reader.GetOrdinal(name);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }

            int? SafeGetInt(string name)
            {
                var ordinal = reader.GetOrdinal(name);
                return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
            }

            decimal? SafeGetDecimal(string name)
            {
                var ordinal = reader.GetOrdinal(name);
                return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
            }

            DateTime? SafeGetDateTime(string name)
            {
                var ordinal = reader.GetOrdinal(name);
                return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
            }

            return new InternalPlatform
            {
                Id = reader.GetInt32(reader.GetOrdinal("ID")),
                AppName = SafeGetString("App_Name"),

                // map ID properties (not navigation objects)
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

                // Use the property name that exists on your model (APPOwner)
                APPOwner = SafeGetString("APP_OP_Owner"),
                AppBusinessOwner = SafeGetString("App_Business_Owner"),
                Price = SafeGetDecimal("Price"),
                EndUserTypeId = SafeGetInt("EndUserType"),
                RequestNo = SafeGetString("RequestNo"),
                ParentProjectID = SafeGetInt("ParentProjectID"),
                SLA = SafeGetString("SLA"),
                MainAppID = SafeGetInt("MainAppID"),
                SSLCertificateExpDate = SafeGetDateTime("SSLCertificateExpDate"),

                // joined display fields
                DevelopedByName = SafeGetString("DevelopedByName"),
                SDLCPhaseName = SafeGetString("SDLCPhaseName"),
                EndUserTypeName = SafeGetString("EndUserTypeName"),
                MainAppName = SafeGetString("MainAppName"),
                ParentProjectGroupName = SafeGetString("ParentProjectGroupName")
            };
        }

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
        }
    }
}
