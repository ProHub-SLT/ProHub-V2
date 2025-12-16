using MySql.Data.MySqlClient;
using ProHub.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ProHub.Data
{
    public class CompanyRepository
    {
        private readonly IConfiguration _configuration;

        public CompanyRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
            => new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // Helper method to safely get values from reader
        private T GetValueOrDefault<T>(MySqlDataReader reader, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return defaultValue;

                var value = reader.GetValue(ordinal);

                if (typeof(T) == typeof(int))
                {
                    return (T)(object)Convert.ToInt32(value);
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // --- CRUD Methods for Company ---

        public List<Company> GetCompanies(string search = "")
        {
            var list = new List<Company>();
            using var conn = GetConnection();
            conn.Open();

            string query = "SELECT ID, Company_Name FROM company";

            if (!string.IsNullOrEmpty(search))
            {
                query += " WHERE Company_Name LIKE @search";
            }

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
            {
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Company
                {
                    Id = GetValueOrDefault<int>(reader, "ID"),
                    CompanyName = GetValueOrDefault<string>(reader, "Company_Name")
                });
            }
            return list.OrderBy(c => c.CompanyName).ToList();
        }

        public Company GetCompanyById(int id)
        {
            Company company = null;
            using var conn = GetConnection();
            conn.Open();

            string query = "SELECT ID, Company_Name FROM company WHERE ID = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                company = new Company
                {
                    Id = GetValueOrDefault<int>(reader, "ID"),
                    CompanyName = GetValueOrDefault<string>(reader, "Company_Name")
                };
            }
            return company;
        }

        public void CreateCompany(Company company)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = "INSERT INTO company (Company_Name) VALUES (@CompanyName)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CompanyName", company.CompanyName);
            cmd.ExecuteNonQuery();
        }

        public void UpdateCompany(Company company)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = "UPDATE company SET Company_Name = @CompanyName WHERE ID = @Id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", company.Id);
            cmd.Parameters.AddWithValue("@CompanyName", company.CompanyName);
            cmd.ExecuteNonQuery();
        }

        public void DeleteCompany(int id)
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "DELETE FROM company WHERE ID = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        // ==========================================
        // NEW METHOD: Get Customer Contacts
        // ==========================================
        public List<CustomerContact> GetContactsByCompanyId(int companyId)
        {
            var list = new List<CustomerContact>();
            using var conn = GetConnection();
            conn.Open();

            // Updated table name to "CustomerContacts" (PascalCase) to match your other repository
            string query = @"SELECT ID, Contact_Name, Contact_Email, Contact_Phone1, Contact_Designation 
                             FROM CustomerContacts 
                             WHERE Contact_Company = @CompanyId";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@CompanyId", companyId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CustomerContact
                {
                    ID = GetValueOrDefault<int>(reader, "ID"),
                    Contact_Name = GetValueOrDefault<string>(reader, "Contact_Name"),
                    Contact_Email = GetValueOrDefault<string>(reader, "Contact_Email"),
                    Contact_Phone1 = GetValueOrDefault<string>(reader, "Contact_Phone1"),
                    Contact_Designation = GetValueOrDefault<string>(reader, "Contact_Designation")
                });
            }
            return list;
        }
    }
}