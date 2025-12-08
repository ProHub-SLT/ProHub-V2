using MySql.Data.MySqlClient;
using ProHub.Models;
using ProHub.Data.Interfaces;

namespace ProHub.Data.Repositories
{
    public class EmployeePermissionRepository : IEmployeePermissionRepository
    {
        private readonly string _connectionString;

        public EmployeePermissionRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public Employee GetEmployeeByEmail(string email)
        {
            Employee emp = null;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string query = "SELECT * FROM Employee WHERE Emp_Email = @Email LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                emp = new Employee
                {
                    EmpId = reader.GetInt32("Emp_ID"),
                    EmpName = reader.GetString("Emp_Name"),
                    EmpEmail = reader.GetString("Emp_Email"),
                    EmpPhone = reader["Emp_Phone"]?.ToString(),
                    Section = reader["Section"]?.ToString()
                };
            }

            return emp;
        }
    }
}
