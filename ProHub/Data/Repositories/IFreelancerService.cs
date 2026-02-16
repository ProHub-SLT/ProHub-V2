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
                SELECT f.*, t.taskid, t.id, t.taskname, t.specification, t.payment, 
                       t.deliveryduedate, t.status, t.paid, t.freelancerid
                FROM freelancers f
                LEFT JOIN tasks t ON f.freelancerid = t.freelancerid
                ORDER BY f.freelancerid, t.taskid";

            using var command = new MySqlCommand(query, connection);
            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            Freelancer? currentFreelancer = null;
            int currentFreelancerId = -1;

            while (await reader.ReadAsync())
            {
                int freelancerId = reader.GetInt32(reader.GetOrdinal("freelancerid"));

                if (freelancerId != currentFreelancerId)
                {
                    currentFreelancer = new Freelancer
                    {
                        FreelancerId = freelancerId,
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        NIC = reader.GetString(reader.GetOrdinal("nic")),
                        ProjectName = reader.GetString(reader.GetOrdinal("projectname")),
                        ProjectScope = reader.IsDBNull(reader.GetOrdinal("projectscope")) ? "" : reader.GetString("projectscope"),
                        Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? "" : reader.GetString("amount"),
                        BudgetAvailable = reader.IsDBNull(reader.GetOrdinal("budgetavailable")) ? "" : reader.GetString("budgetavailable"),
                        StartDate = reader.IsDBNull(reader.GetOrdinal("startdate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("startdate")),
                        EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("enddate")),
                        Duration = reader.IsDBNull(reader.GetOrdinal("duration")) ? "" : reader.GetString("duration"),
                        Tasks = new List<FreelancerTaskViewModel>()
                    };

                    freelancers.Add(currentFreelancer);
                    currentFreelancerId = freelancerId;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("taskid")) && currentFreelancer != null)
                {
                    var task = new FreelancerTaskViewModel
                    {
                        TaskId = reader.GetInt32("taskid"),
                        ID = reader.GetInt32("id"),
                        TaskName = reader.IsDBNull(reader.GetOrdinal("taskname")) ? "" : reader.GetString("taskname"),
                        Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? "" : reader.GetString("specification"),
                        Payment = reader.IsDBNull(reader.GetOrdinal("payment")) ? "" : reader.GetString("payment"),
                        DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("deliveryduedate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("deliveryduedate")),
                        Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString("status"),
                        Paid = reader.IsDBNull(reader.GetOrdinal("paid")) ? "No" : reader.GetString("paid")
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
                SELECT f.*, t.taskid, t.id, t.taskname, t.specification, t.payment, 
                       t.deliveryduedate, t.status, t.paid, t.freelancerid
                FROM freelancers f
                LEFT JOIN tasks t ON f.freelancerid = t.freelancerid
                WHERE f.freelancerid = @FreelancerId
                ORDER BY t.taskid";

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
                        FreelancerId = reader.GetInt32(reader.GetOrdinal("freelancerid")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        NIC = reader.GetString(reader.GetOrdinal("nic")),
                        ProjectName = reader.GetString(reader.GetOrdinal("projectname")),
                        ProjectScope = reader.IsDBNull(reader.GetOrdinal("projectscope")) ? "" : reader.GetString("projectscope"),
                        Amount = reader.IsDBNull(reader.GetOrdinal("amount")) ? "" : reader.GetString("amount"),
                        BudgetAvailable = reader.IsDBNull(reader.GetOrdinal("budgetavailable")) ? "" : reader.GetString("budgetavailable"),
                        StartDate = reader.IsDBNull(reader.GetOrdinal("startdate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("startdate")),
                        EndDate = reader.IsDBNull(reader.GetOrdinal("enddate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("enddate")),
                        Duration = reader.IsDBNull(reader.GetOrdinal("duration")) ? "" : reader.GetString("duration"),
                        Tasks = new List<FreelancerTaskViewModel>()
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("taskid")))
                {
                    var task = new FreelancerTaskViewModel
                    {
                        TaskId = reader.GetInt32("taskid"),
                        ID = reader.GetInt32("id"),
                        TaskName = reader.IsDBNull(reader.GetOrdinal("taskname")) ? "" : reader.GetString("taskname"),
                        Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? "" : reader.GetString("specification"),
                        Payment = reader.IsDBNull(reader.GetOrdinal("payment")) ? "" : reader.GetString("payment"),
                        DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("deliveryduedate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("deliveryduedate")),
                        Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString("status"),
                        Paid = reader.IsDBNull(reader.GetOrdinal("paid")) ? "No" : reader.GetString("paid")
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
                    INSERT INTO freelancers (name, nic, projectname, projectscope, amount, budgetavailable, startdate, enddate, duration)
                    VALUES (@Name, @NIC, @ProjectName, @ProjectScope, @Amount, @BudgetAvailable, @StartDate, @EndDate, @Duration);
                    SELECT LAST_INSERT_ID();";

                using var freelancerCommand = new MySqlCommand(freelancerQuery, connection, transaction);
                AddFreelancerParameters(freelancerCommand, freelancer);

                var freelancerId = Convert.ToInt32(await freelancerCommand.ExecuteScalarAsync());

                foreach (var task in freelancer.Tasks)
                {
                    const string taskQuery = @"
                        INSERT INTO tasks (id, taskname, specification, payment, deliveryduedate, status, paid, freelancerid)
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
                    SET name=@Name, nic=@NIC, projectname=@ProjectName, projectscope=@ProjectScope,
                        amount=@Amount, budgetavailable=@BudgetAvailable, startdate=@StartDate, 
                        enddate=@EndDate, duration=@Duration
                    WHERE freelancerid=@FreelancerId";

                using var freelancerCommand = new MySqlCommand(freelancerQuery, connection, transaction);
                freelancerCommand.Parameters.AddWithValue("@FreelancerId", freelancer.FreelancerId);
                AddFreelancerParameters(freelancerCommand, freelancer);

                await freelancerCommand.ExecuteNonQueryAsync();

                // Refresh tasks
                const string deleteTasks = "DELETE FROM tasks WHERE freelancerid = @FreelancerId";
                using var deleteCommand = new MySqlCommand(deleteTasks, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@FreelancerId", freelancer.FreelancerId);
                await deleteCommand.ExecuteNonQueryAsync();

                foreach (var task in freelancer.Tasks)
                {
                    const string insertTask = @"
                        INSERT INTO tasks (id, taskname, specification, payment, deliveryduedate, status, paid, freelancerid)
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

            const string query = "DELETE FROM freelancers WHERE freelancerid = @FreelancerId";
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

            const string query = "SELECT taskid, id, taskname, specification, payment, deliveryduedate, status, paid, freelancerid FROM tasks WHERE freelancerid = @FreelancerId";
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@FreelancerId", freelancerId);

            using var reader = (MySqlDataReader)await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tasks.Add(new FreelancerTaskViewModel
                {
                    TaskId = reader.GetInt32("taskid"),
                    ID = reader.GetInt32("id"),
                    TaskName = reader.IsDBNull(reader.GetOrdinal("taskname")) ? "" : reader.GetString("taskname"),
                    Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? "" : reader.GetString("specification"),
                    Payment = reader.IsDBNull(reader.GetOrdinal("payment")) ? "" : reader.GetString("payment"),
                    DeliveryDueDate = reader.IsDBNull(reader.GetOrdinal("deliveryduedate")) ? null : DateOnly.FromDateTime(reader.GetDateTime("deliveryduedate")),
                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Pending" : reader.GetString("status"),
                    Paid = reader.IsDBNull(reader.GetOrdinal("paid")) ? "No" : reader.GetString("paid")
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