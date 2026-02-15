using MySql.Data.MySqlClient;
using PROHUB.Models;

namespace PROHUB.Services
{
    public interface IFreelancerService
    {
        Task<List<Freelancer>> GetAllFreelancersAsync();
        Task<Freelancer?> GetFreelancerByIdAsync(int freelancerId);
        Task<int> AddFreelancerAsync(Freelancer freelancer);
        Task<bool> UpdateFreelancerAsync(Freelancer freelancer);
        Task<bool> DeleteFreelancerAsync(int freelancerId);
        Task<List<FreelancerTaskViewModel>> GetTasksByFreelancerIdAsync(int freelancerId);
    }

    public class FreelancerService : IFreelancerService
    {
        private readonly string _connectionString;

        public FreelancerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is missing or empty in configuration.");
            }
        }

        // ---------------------------
        // GET ALL FREELANCERS
        // ---------------------------
        public async Task<List<Freelancer>> GetAllFreelancersAsync()
        {
            var freelancers = new List<Freelancer>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT f.*, t.TaskId, t.ID, t.TaskName, t.Specification, t.Payment, 
                       t.DeliveryDueDate, t.Status, t.Paid, t.FreelancerId
                FROM freelancers f
                LEFT JOIN tasks t ON f.FreelancerId = t.FreelancerId
                ORDER BY f.FreelancerId, t.TaskId";

            using var command = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            Freelancer? currentFreelancer = null;
            int currentFreelancerId = -1;

            while (await reader.ReadAsync())
            {
                int freelancerId = reader.GetInt32(reader.GetOrdinal("FreelancerId"));

                if (freelancerId != currentFreelancerId)
                {
                    currentFreelancer = new Freelancer
                    {
                        FreelancerId = freelancerId,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        NIC = reader.GetString(reader.GetOrdinal("NIC")),
                        ProjectName = reader.GetString(reader.GetOrdinal("ProjectName")),
                        ProjectScope = reader.IsDBNull(reader.GetOrdinal("ProjectScope")) ? "" : reader.GetString("ProjectScope"),
                        Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? "" : reader.GetString("Amount"),
                        BudgetAvailable = reader.IsDBNull(reader.GetOrdinal("BudgetAvailable")) ? "" : reader.GetString("BudgetAvailable"),
                        StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("StartDate")),
                        EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("EndDate")),
                        Duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? "" : reader.GetString("Duration"),
                        Tasks = new List<FreelancerTaskViewModel>()
                    };

                    freelancers.Add(currentFreelancer);
                    currentFreelancerId = freelancerId;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("TaskId")) && currentFreelancer != null)
                {
                    var task = new FreelancerTaskViewModel
                    {
                        TaskId = reader.GetInt32("TaskId"),
                        ID = reader.GetInt32("ID"),
                        TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? "" : reader.GetString("TaskName"),
                        Specification = reader.IsDBNull(reader.GetOrdinal("Specification")) ? "" : reader.GetString("Specification"),
                        Payment = reader.IsDBNull(reader.GetOrdinal("Payment")) ? "" : reader.GetString("Payment"),
                        DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("DeliveryDueDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("DeliveryDueDate")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Pending" : reader.GetString("Status"),
                        Paid = reader.IsDBNull(reader.GetOrdinal("Paid")) ? "No" : reader.GetString("Paid")
                    };

                    currentFreelancer.Tasks.Add(task);
                }
            }

            return freelancers;
        }

        // ---------------------------
        // GET FREELANCER BY ID
        // ---------------------------
        public async Task<Freelancer?> GetFreelancerByIdAsync(int freelancerId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                SELECT f.*, t.TaskId, t.ID, t.TaskName, t.Specification, t.Payment, 
                       t.DeliveryDueDate, t.Status, t.Paid, t.FreelancerId
                FROM freelancers f
                LEFT JOIN tasks t ON f.FreelancerId = t.FreelancerId
                WHERE f.FreelancerId = @FreelancerId
                ORDER BY t.TaskId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@FreelancerId", freelancerId);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            Freelancer? freelancer = null;

            while (await reader.ReadAsync())
            {
                if (freelancer == null)
                {
                    freelancer = new Freelancer
                    {
                        FreelancerId = reader.GetInt32(reader.GetOrdinal("FreelancerId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        NIC = reader.GetString(reader.GetOrdinal("NIC")),
                        ProjectName = reader.GetString(reader.GetOrdinal("ProjectName")),
                        ProjectScope = reader.IsDBNull(reader.GetOrdinal("ProjectScope")) ? "" : reader.GetString("ProjectScope"),
                        Amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? "" : reader.GetString("Amount"),
                        BudgetAvailable = reader.IsDBNull(reader.GetOrdinal("BudgetAvailable")) ? "" : reader.GetString("BudgetAvailable"),
                        StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("StartDate")),
                        EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("EndDate")),
                        Duration = reader.IsDBNull(reader.GetOrdinal("Duration")) ? "" : reader.GetString("Duration"),
                        Tasks = new List<FreelancerTaskViewModel>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("TaskId")))
                {
                    var task = new FreelancerTaskViewModel
                    {
                        TaskId = reader.GetInt32("TaskId"),
                        ID = reader.GetInt32("ID"),
                        TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? "" : reader.GetString("TaskName"),
                        Specification = reader.IsDBNull(reader.GetOrdinal("Specification")) ? "" : reader.GetString("Specification"),
                        Payment = reader.IsDBNull(reader.GetOrdinal("Payment")) ? "" : reader.GetString("Payment"),
                        DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("DeliveryDueDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("DeliveryDueDate")),
                        Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Pending" : reader.GetString("Status"),
                        Paid = reader.IsDBNull(reader.GetOrdinal("Paid")) ? "No" : reader.GetString("Paid")
                    };

                    freelancer.Tasks.Add(task);
                }
            }

            return freelancer;
        }

        // ---------------------------
        // ADD FREELANCER
        // ---------------------------
        public async Task<int> AddFreelancerAsync(Freelancer freelancer)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string freelancerQuery = @"
                    INSERT INTO freelancers (Name, NIC, ProjectName, ProjectScope, Amount, BudgetAvailable, StartDate, EndDate, Duration)
                    VALUES (@Name, @NIC, @ProjectName, @ProjectScope, @Amount, @BudgetAvailable, @StartDate, @EndDate, @Duration);
                    SELECT LAST_INSERT_ID();";

                using var freelancerCommand = new MySqlCommand(freelancerQuery, connection, transaction);
                AddFreelancerParameters(freelancerCommand, freelancer);

                var freelancerId = Convert.ToInt32(await freelancerCommand.ExecuteScalarAsync());

                foreach (var task in freelancer.Tasks)
                {
                    const string taskQuery = @"
                        INSERT INTO tasks (ID, TaskName, Specification, Payment, DeliveryDueDate, Status, Paid, FreelancerId)
                        VALUES (@ID, @TaskName, @Specification, @Payment, @DeliveryDueDate, @Status, @Paid, @FreelancerId)";

                    using var taskCommand = new MySqlCommand(taskQuery, connection, transaction);
                    AddTaskParameters(taskCommand, task, freelancerId);

                    await taskCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return freelancerId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ---------------------------
        // UPDATE FREELANCER
        // ---------------------------
        public async Task<bool> UpdateFreelancerAsync(Freelancer freelancer)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                const string freelancerQuery = @"
                    UPDATE freelancers 
                    SET Name=@Name, NIC=@NIC, ProjectName=@ProjectName, ProjectScope=@ProjectScope,
                        Amount=@Amount, BudgetAvailable=@BudgetAvailable, StartDate=@StartDate, 
                        EndDate=@EndDate, Duration=@Duration
                    WHERE FreelancerId=@FreelancerId";

                using var freelancerCommand = new MySqlCommand(freelancerQuery, connection, transaction);
                freelancerCommand.Parameters.AddWithValue("@FreelancerId", freelancer.FreelancerId);
                AddFreelancerParameters(freelancerCommand, freelancer);

                await freelancerCommand.ExecuteNonQueryAsync();

                // Refresh tasks
                const string deleteTasks = "DELETE FROM Tasks WHERE FreelancerId = @FreelancerId";
                using var deleteCommand = new MySqlCommand(deleteTasks, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@FreelancerId", freelancer.FreelancerId);
                await deleteCommand.ExecuteNonQueryAsync();

                foreach (var task in freelancer.Tasks)
                {
                    const string insertTask = @"
                        INSERT INTO tasks (ID, TaskName, Specification, Payment, DeliveryDueDate, Status, Paid, FreelancerId)
                        VALUES (@ID, @TaskName, @Specification, @Payment, @DeliveryDueDate, @Status, @Paid, @FreelancerId)";

                    using var taskCommand = new MySqlCommand(insertTask, connection, transaction);
                    AddTaskParameters(taskCommand, task, freelancer.FreelancerId);

                    await taskCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        // ---------------------------
        // DELETE FREELANCER
        // ---------------------------
        public async Task<bool> DeleteFreelancerAsync(int freelancerId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM freelancers WHERE FreelancerId = @FreelancerId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@FreelancerId", freelancerId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // ---------------------------
        // GET TASKS BY FREELANCER
        // ---------------------------
        public async Task<List<FreelancerTaskViewModel>> GetTasksByFreelancerIdAsync(int freelancerId)
        {
            var tasks = new List<FreelancerTaskViewModel>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "SELECT * FROM tasks WHERE FreelancerId = @FreelancerId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@FreelancerId", freelancerId);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(new FreelancerTaskViewModel
                {
                    TaskId = reader.GetInt32("TaskId"),
                    ID = reader.GetInt32("ID"),
                    TaskName = reader.IsDBNull(reader.GetOrdinal("TaskName")) ? "" : reader.GetString("TaskName"),
                    Specification = reader.IsDBNull(reader.GetOrdinal("Specification")) ? "" : reader.GetString("Specification"),
                    Payment = reader.IsDBNull(reader.GetOrdinal("Payment")) ? "" : reader.GetString("Payment"),
                    DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("DeliveryDueDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("DeliveryDueDate")),
                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Pending" : reader.GetString("Status"),
                    Paid = reader.IsDBNull(reader.GetOrdinal("Paid")) ? "No" : reader.GetString("Paid")
                });
            }

            return tasks;
        }

        // ---------------------------
        // HELPER METHODS
        // ---------------------------
        private void AddFreelancerParameters(MySqlCommand command, Freelancer freelancer)
        {
            command.Parameters.AddWithValue("@Name", freelancer.Name);
            command.Parameters.AddWithValue("@NIC", freelancer.NIC);
            command.Parameters.AddWithValue("@ProjectName", freelancer.ProjectName);
            command.Parameters.AddWithValue("@ProjectScope", freelancer.ProjectScope ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Amount", freelancer.Amount ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BudgetAvailable", freelancer.BudgetAvailable ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", freelancer.StartDate?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", freelancer.EndDate?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Duration", freelancer.Duration ?? (object)DBNull.Value);
        }

        private void AddTaskParameters(MySqlCommand command, FreelancerTaskViewModel task, int freelancerId)
        {
            command.Parameters.AddWithValue("@ID", task.ID);
            command.Parameters.AddWithValue("@TaskName", task.TaskName);
            command.Parameters.AddWithValue("@Specification", task.Specification ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Payment", task.Payment ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DeliveryDueDate", task.DeliveryDueDate?.ToDateTime(TimeOnly.MinValue) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Status", task.Status);
            command.Parameters.AddWithValue("@Paid", task.Paid);
            command.Parameters.AddWithValue("@FreelancerId", freelancerId);
        }
    }
}