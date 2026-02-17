using MySql.Data.MySqlClient;
using System.Text.Json;
using ProHub.Data;
using ProHub.Models;

namespace ProHub.Data
{
    public class NextWeekPlanRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ExternalSolutionRepository _extRepo;
        private readonly ConsumerPlatformRepository _intRepo;
        private readonly EmployeeRepository _empRepo;

        public NextWeekPlanRepository(IConfiguration configuration,
            ExternalSolutionRepository extRepo,
            ConsumerPlatformRepository intRepo,
            EmployeeRepository empRepo)
        {
            _configuration = configuration;
            _extRepo = extRepo;
            _intRepo = intRepo;
            _empRepo = empRepo;
        }

        private MySqlConnection GetConnection() => new(_configuration.GetConnectionString("DefaultConnection"));

        public List<Models.NextWeekPlan> GetByWeek(DateTime start)
        {
            var list = new List<Models.NextWeekPlan>();
            using var conn = GetConnection();
            conn.Open();

            // Updated query to match existing database schema
            string sql = @"
                SELECT ID, StartDate, EndDate, ExternalPlatform, InternalApp,
                       WorkPlanDesc, UpdatedBy, UpdatedOn
                FROM WorkPlan
                WHERE StartDate = @start
                ORDER BY UpdatedOn DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@start", start.Date);
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                int externalPlatformCol = r.GetOrdinal("ExternalPlatform");
                int internalAppCol = r.GetOrdinal("InternalApp");
                int workPlanDescCol = r.GetOrdinal("WorkPlanDesc");
                int updatedByCol = r.GetOrdinal("UpdatedBy");
                int updatedOnCol = r.GetOrdinal("UpdatedOn");

                var plan = new NextWeekPlan
                {
                    ID = r.GetInt32("ID"),
                    StartDate = r.GetDateTime("StartDate"),
                    EndDate = r.GetDateTime("EndDate"),
                    ExternalPlatform = r.IsDBNull(externalPlatformCol) ? null : r.GetInt32(externalPlatformCol),
                    InternalApp = r.IsDBNull(internalAppCol) ? null : r.GetInt32(internalAppCol),
                    WorkPlanDesc = r.IsDBNull(workPlanDescCol) ? null : r.GetString(workPlanDescCol),
                    UpdatedBy = r.IsDBNull(updatedByCol) ? null : r.GetInt32(updatedByCol),
                    UpdatedOn = r.GetDateTime(updatedOnCol)
                };

                plan.UpdatedByName = plan.UpdatedBy.HasValue ? _empRepo.GetNameById(plan.UpdatedBy.Value) : "Unknown";
                // Populate the platform lists for display in the view
                plan.ExternalPlatforms = _extRepo.GetAll();
                plan.InternalPlatforms = _intRepo.GetAll();
                list.Add(plan);
            }
            return list;
        }

        public void Create(NextWeekPlan plan)
        {
            using var conn = GetConnection();
            conn.Open();
            // Updated query to match existing database schema
            string sql = @"
                INSERT INTO WorkPlan 
                (StartDate, EndDate, ExternalPlatform, InternalApp,
                 WorkPlanDesc, UpdatedBy, UpdatedOn)
                VALUES (@s, @e, @ep, @ia, @wp, @ub, NOW())";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s", plan.StartDate);
            cmd.Parameters.AddWithValue("@e", plan.EndDate);
            cmd.Parameters.AddWithValue("@ep", plan.ExternalPlatform ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ia", plan.InternalApp ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wp", plan.WorkPlanDesc ?? "");
            cmd.Parameters.AddWithValue("@ub", plan.UpdatedBy ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM WorkPlan WHERE ID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void PermanentDelete(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM WorkPlan WHERE ID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // Add the GetById method for the edit feature
        public NextWeekPlan? GetById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            // Updated query to match existing database schema
            string sql = @"
                SELECT ID, StartDate, EndDate, ExternalPlatform, InternalApp,
                       WorkPlanDesc, UpdatedBy, UpdatedOn
                FROM WorkPlan
                WHERE ID = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();

            if (r.Read())
            {
                int externalPlatformCol = r.GetOrdinal("ExternalPlatform");
                int internalAppCol = r.GetOrdinal("InternalApp");
                int workPlanDescCol = r.GetOrdinal("WorkPlanDesc");
                int updatedByCol = r.GetOrdinal("UpdatedBy");

                var plan = new NextWeekPlan
                {
                    ID = r.GetInt32("ID"),
                    StartDate = r.GetDateTime("StartDate"),
                    EndDate = r.GetDateTime("EndDate"),
                    ExternalPlatform = r.IsDBNull(externalPlatformCol) ? null : r.GetInt32(externalPlatformCol),
                    InternalApp = r.IsDBNull(internalAppCol) ? null : r.GetInt32(internalAppCol),
                    WorkPlanDesc = r.IsDBNull(workPlanDescCol) ? null : r.GetString(workPlanDescCol),
                    UpdatedBy = r.IsDBNull(updatedByCol) ? null : r.GetInt32(updatedByCol),
                    UpdatedOn = r.GetDateTime("UpdatedOn")
                };
                plan.UpdatedByName = plan.UpdatedBy.HasValue ? _empRepo.GetNameById(plan.UpdatedBy.Value) : "Unknown";
                // Populate the platform lists for display in the view
                plan.ExternalPlatforms = _extRepo.GetAll();
                plan.InternalPlatforms = _intRepo.GetAll();
                return plan;
            }

            return null;
        }

        // Add the Update method for the edit feature
        public void Update(NextWeekPlan plan)
        {
            using var conn = GetConnection();
            conn.Open();

            // Updated query to match existing database schema
            string sql = @"
                UPDATE WorkPlan 
                SET StartDate = @s, 
                    EndDate = @e, 
                    ExternalPlatform = @ep, 
                    InternalApp = @ia,
                    WorkPlanDesc = @wp, 
                    UpdatedBy = @ub, 
                    UpdatedOn = NOW()
                WHERE ID = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", plan.ID);
            cmd.Parameters.AddWithValue("@s", plan.StartDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@e", plan.EndDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ep", plan.ExternalPlatform ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ia", plan.InternalApp ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@wp", plan.WorkPlanDesc ?? "");
            cmd.Parameters.AddWithValue("@ub", plan.UpdatedBy ?? (object)DBNull.Value);

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                throw new Exception("Plan not found or already deleted.");
            }
        }
    }
}