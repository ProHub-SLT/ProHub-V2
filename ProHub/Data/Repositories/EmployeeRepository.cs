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
            using var cmd = new MySqlCommand("SELECT Emp_Name FROM employee WHERE Emp_ID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteScalar()?.ToString() ?? "Unknown";
        }
        
        public int GetIdByName(string name)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_ID FROM employee WHERE Emp_Name = @name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public Dictionary<int, string> GetNamesByIds(IEnumerable<int> ids)
        {
            if (!ids.Any()) return new();

            using var conn = GetConnection();
            conn.Open();

            var idList = ids.ToList();
            var placeholders = string.Join(",", idList.Select((_, i) => $"@p{i}"));

            var sql = $@"
                SELECT e.Emp_ID, e.Emp_Name
                FROM employee e
                LEFT JOIN empgroup g ON e.GroupID = g.GroupID
                WHERE e.Emp_ID IN ({placeholders})
                  AND (g.GroupName IS NULL OR g.GroupName <> 'Inactive')
            ";

            using var cmd = new MySqlCommand(sql, conn);
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.AddWithValue($"@p{i}", idList[i]);

            var dict = new Dictionary<int, string>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                dict[r.GetInt32(0)] = r.GetString(1);

            return dict;
        }


        public List<int> GetAllEmployeeIds()
        {
            var ids = new List<int>();

            using var conn = GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(@"
                SELECT e.Emp_ID
                FROM employee e
                LEFT JOIN empgroup g ON e.GroupID = g.GroupID
                WHERE g.GroupName IS NULL
                   OR g.GroupName <> 'Inactive'
                ORDER BY e.Emp_Name
            ", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                ids.Add(r.GetInt32(0));

            return ids;
        }


        public string GetEmployeeNameByEmail(string email)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_Name FROM employee WHERE Emp_Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            return cmd.ExecuteScalar()?.ToString() ?? "Unknown User";
        }
        
        public int GetEmployeeIdByEmail(string email)
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT Emp_ID FROM employee WHERE Emp_Email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
    
    
}