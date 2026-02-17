using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using System;
using System.Collections.Generic;

namespace ProHub.Data
{
    public class CustomerContactRepository
    {
        private readonly IConfiguration _configuration;

        public CustomerContactRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // Helper method to safely get values from the reader
        private T GetValueOrDefault<T>(MySqlDataReader reader, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return defaultValue;
                var value = reader.GetValue(ordinal);
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        // ✅ Get all Customer Contacts (Index View)
        public List<CustomerContact> GetCustomerContacts(string search = "")
        {
            var list = new List<CustomerContact>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    cc.Id,
                    cc.Customer_Title,
                    cc.Contact_Name,
                    cc.Contact_Phone1,
                    cc.Contact_Company,
                    c.Company_Name,
                    cc.Platform_ID,
                    ep.Platform_Name
                FROM customercontacts cc
                LEFT JOIN Company c ON cc.Contact_Company = c.Id
                LEFT JOIN external_platforms ep ON cc.Platform_ID = ep.Id
                WHERE 1=1";

            if (!string.IsNullOrEmpty(search))
                query += " AND (cc.Contact_Name LIKE @search OR c.Company_Name LIKE @search OR ep.Platform_Name LIKE @search)";

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("@search", $"%{search}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CustomerContact
                {
                    ID = GetValueOrDefault(reader, "Id", 0),
                    Customer_Title = GetValueOrDefault(reader, "Customer_Title", ""),
                    Contact_Name = GetValueOrDefault(reader, "Contact_Name", ""),
                    Contact_Phone1 = GetValueOrDefault(reader, "Contact_Phone1", ""),
                    Platform_ID = GetValueOrDefault(reader, "Platform_ID", (int?)null),
                    Contact_Company = GetValueOrDefault(reader, "Contact_Company", (int?)null),

                    // Navigation objects
                    Platform = new ExternalPlatform
                    {
                        Id = GetValueOrDefault(reader, "Platform_ID", 0),
                        PlatformName = GetValueOrDefault(reader, "Platform_Name", "")
                    },
                    Company = new Company
                    {
                        Id = GetValueOrDefault(reader, "Contact_Company", 0),
                        CompanyName = GetValueOrDefault(reader, "Company_Name", "")
                    }
                });
            }

            return list;
        }

        // ✅ Get single Customer Contact by ID (Details View)
        public CustomerContact GetCustomerContactById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    cc.Id,
                    cc.Platform_ID,
                    ep.Platform_Name,
                    cc.Customer_Title,
                    cc.Contact_Name,
                    cc.Contact_Phone1,
                    cc.Contact_Phone2,
                    cc.Contact_Email,
                    cc.Contact_Designation,
                    cc.Contact_Company,
                    c.Company_Name
                FROM customercontacts cc
                LEFT JOIN Company c ON cc.Contact_Company = c.Id
                LEFT JOIN external_platforms ep ON cc.Platform_ID = ep.Id
                WHERE cc.Id = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new CustomerContact
                {
                    ID = GetValueOrDefault(reader, "Id", 0),
                    Platform_ID = GetValueOrDefault(reader, "Platform_ID", (int?)null),
                    Customer_Title = GetValueOrDefault(reader, "Customer_Title", ""),
                    Contact_Name = GetValueOrDefault(reader, "Contact_Name", ""),
                    Contact_Phone1 = GetValueOrDefault(reader, "Contact_Phone1", ""),
                    Contact_Phone2 = GetValueOrDefault(reader, "Contact_Phone2", ""),
                    Contact_Email = GetValueOrDefault(reader, "Contact_Email", ""),
                    Contact_Designation = GetValueOrDefault(reader, "Contact_Designation", ""),
                    Contact_Company = GetValueOrDefault(reader, "Contact_Company", (int?)null),

                    // Navigation objects
                    Platform = new ExternalPlatform
                    {
                        Id = GetValueOrDefault(reader, "Platform_ID", 0),
                        PlatformName = GetValueOrDefault(reader, "Platform_Name", "")
                    },
                    Company = new Company
                    {
                        Id = GetValueOrDefault(reader, "Contact_Company", 0),
                        CompanyName = GetValueOrDefault(reader, "Company_Name", "")
                    }
                };
            }

            return null;
        }
    }
}
