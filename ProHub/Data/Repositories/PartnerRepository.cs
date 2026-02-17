using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using System.Collections.Generic;

namespace ProHub.Data
{
    public class PartnerRepository
    {
        private readonly IConfiguration _configuration;

        public PartnerRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ Get all partners with optional search
        public List<Partner> GetAllPartners(string search = "")
        {
            var list = new List<Partner>();
            using var conn = GetConnection();
            conn.Open();

            string query = "SELECT * FROM partner WHERE 1=1";
            if (!string.IsNullOrEmpty(search))
            {
                query += " AND (Partner_Name LIKE @search OR Partner_Organization LIKE @search OR Partner_Title LIKE @search)";
            }

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
            {
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Partner
                {
                    ID = reader.GetInt32("ID"),
                    Partner_Organization = reader["Partner_Organization"]?.ToString(),
                    Partner_Title = reader["Partner_Title"]?.ToString(),
                    Partner_Name = reader["Partner_Name"]?.ToString(),
                    Partner_Phone1 = reader["Partner_Phone1"]?.ToString(),
                    Partner_Phone2 = reader["Partner_Phone2"]?.ToString(),
                    Partner_Email = reader["Partner_Email"]?.ToString(),
                    Partner_Designation = reader["Partner_Designation"]?.ToString()
                });
            }

            return list;
        }

        // ✅ Add new partners
        public void AddPartner(Partner partner)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"INSERT INTO partner 
                             (Partner_Organization, Partner_Title, Partner_Name, Partner_Phone1, Partner_Phone2, Partner_Email, Partner_Designation)
                             VALUES (@org, @title, @name, @ph1, @ph2, @email, @desig)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@org", partner.Partner_Organization);
            cmd.Parameters.AddWithValue("@title", partner.Partner_Title);
            cmd.Parameters.AddWithValue("@name", partner.Partner_Name);
            cmd.Parameters.AddWithValue("@ph1", partner.Partner_Phone1);
            cmd.Parameters.AddWithValue("@ph2", partner.Partner_Phone2);
            cmd.Parameters.AddWithValue("@email", partner.Partner_Email);
            cmd.Parameters.AddWithValue("@desig", partner.Partner_Designation);
            cmd.ExecuteNonQuery();
        }
    }
}
