using MySql.Data.MySqlClient;

namespace ProHub.Data
{


    public class EmployeeRepository
    {
        private readonly IConfiguration _configuration;
        public EmployeeRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection() => new(_configuration.GetConnectionString("DefaultConnection"));

        public string GetNameById(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_Name FROM Employee WHERE Emp_ID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar()?.ToString() ?? "Unknown";
        }
        
        public int GetIdByName(string name)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_ID FROM Employee WHERE Emp_Name = @name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
        
        public Dictionary<int, string> GetNamesByIds(IEnumerable<int> ids)
        {
            if (!ids.Any()) return new();
            using var conn = GetConnection();
            conn.Open();
            var placeholders = string.Join(",", ids.Select((_, i) => $"@p{i}"));
            var sql = $"SELECT Emp_ID, Emp_Name FROM Employee WHERE Emp_ID IN ({placeholders})";
            using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < ids.Count(); i++)
                cmd.Parameters.AddWithValue($"@p{i}", ids.ElementAt(i));
            var dict = new Dictionary<int, string>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) dict[r.GetInt32(0)] = r.GetString(1);
            return dict;
        }

        public List<int> GetAllEmployeeIds()
        {
            var ids = new List<int>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_ID FROM Employee ORDER BY Emp_Name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(r.GetInt32(0));
            return ids;
        }

        public string GetEmployeeNameByEmail(string email)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_Name FROM Employee WHERE Emp_Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            return cmd.ExecuteScalar()?.ToString() ?? "Unknown User";
        }
        
        public int GetEmployeeIdByEmail(string email)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_ID FROM Employee WHERE Emp_Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
    
    
}