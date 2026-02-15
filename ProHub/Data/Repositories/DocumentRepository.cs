// File: Repositories/DocumentRepository.cs
using MySql.Data.MySqlClient;
using ProHub.Models;
using System;
using System.Collections.Generic;

namespace ProHub.Data
{
    public class DocumentRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ConsumerPlatformRepository _internalRepo;
        private readonly ExternalSolutionRepository _externalRepo;
        private readonly EmployeeRepository _empRepo;

        public DocumentRepository(
            IConfiguration configuration,
            ConsumerPlatformRepository internalRepo,
            ExternalSolutionRepository externalRepo,
            EmployeeRepository empRepo)
        {
            _configuration = configuration;
            _internalRepo = internalRepo;
            _externalRepo = externalRepo;
            _empRepo = empRepo;
        }

        private MySqlConnection GetConnection() => new(_configuration.GetConnectionString("DefaultConnection"));

        // Get the platform ID for "internal" documents
        private int GetInternalPlatformId()
        {
            var internalPlatformId = _internalRepo.GetInternalPlatformId();
            return internalPlatformId ?? 1; // Fallback to 1 if not found in Main_Platforms table
        }

        // Get the platform ID for "external" documents
        private int GetExternalPlatformId()
        {
            var externalPlatformId = _externalRepo.GetExternalPlatformId();
            return externalPlatformId ?? 2; // Fallback to 2 if not found in Main_Platforms table
        }


        //  GetInternalDocuments Method 
        public List<Document> GetInternalDocuments(string search, int? solutionId, int page, int pageSize)
        {
            var list = new List<Document>();
            using var conn = GetConnection();
            conn.Open();

            // Get the correct platform ID for internal documents
            var internalPlatformId = GetInternalPlatformId();


            string sql = @"
        SELECT d.*, ip.App_Name AS SolutionName, e.Emp_Name AS CreatedByName
        FROM Document d
        LEFT JOIN Internal_Platforms ip ON d.Solution_ID = ip.ID
        LEFT JOIN employee e ON d.Created_By = e.Emp_ID
        WHERE d.Platform_ID = @platformId 
          AND (@solutionId IS NULL OR d.Solution_ID = @solutionId)
          AND (d.Doc_Name LIKE @search OR ip.App_Name LIKE @search OR e.Emp_Name LIKE @search)
        ORDER BY d.Created_Time DESC
        LIMIT @offset, @pageSize";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@platformId", internalPlatformId);


            cmd.Parameters.AddWithValue("@solutionId", solutionId ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int solutionNameCol = r.GetOrdinal("SolutionName");
                int createdByNameCol = r.GetOrdinal("CreatedByName");

                var doc = new Document
                {
                    ID = r.GetInt32("ID"),
                    Platform_ID = r.GetInt32("Platform_ID"),
                    Solution_ID = r.IsDBNull(r.GetOrdinal("Solution_ID")) ? null : r.GetInt32("Solution_ID"),
                    Doc_Name = r.GetString("Doc_Name"),
                    Created_Time = r.IsDBNull(r.GetOrdinal("Created_Time")) ? null : r.GetDateTime("Created_Time"),
                    Created_By = r.IsDBNull(r.GetOrdinal("Created_By")) ? null : r.GetInt32("Created_By"),
                    Doc_Classification = r.IsDBNull(r.GetOrdinal("Doc_Classification")) ? null : r.GetString("Doc_Classification"),
                    Tags = r.IsDBNull(r.GetOrdinal("Tags")) ? null : r.GetString("Tags"),
                    Confidential = r.GetBoolean("Confidential"),
                    Doc_URL = r.IsDBNull(r.GetOrdinal("Doc_URL")) ? null : r.GetString("Doc_URL"),
                    SolutionName = r.IsDBNull(solutionNameCol) ? "None" : r.GetString(solutionNameCol),
                    CreatedByName = r.IsDBNull(createdByNameCol) ? "Unknown" : r.GetString(createdByNameCol)
                };

                list.Add(doc);
            }
            return list;
        }


        public int GetInternalDocumentCount(string search, int? solutionId)
        {
            using var conn = GetConnection();
            conn.Open();

            var internalPlatformId = GetInternalPlatformId();


            string sql = @"
        SELECT COUNT(*) 
        FROM Document d
        LEFT JOIN Internal_Platforms ip ON d.Solution_ID = ip.ID
        LEFT JOIN employee e ON d.Created_By = e.Emp_ID
        WHERE d.Platform_ID = @platformId 
          AND (@solutionId IS NULL OR d.Solution_ID = @solutionId)
          AND (d.Doc_Name LIKE @search OR ip.App_Name LIKE @search OR e.Emp_Name LIKE @search)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@platformId", internalPlatformId);


            cmd.Parameters.AddWithValue("@solutionId", solutionId ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }



        // Repositories/DocumentRepository.cs  Methods 

        // 1. GetExternalDocuments Method  'int solutionId' 
        public List<Document> GetExternalDocuments(string search, int? solutionId, int page, int pageSize)
        {
            var list = new List<Document>();
            using var conn = GetConnection();
            conn.Open();

            var externalPlatformId = GetExternalPlatformId();

            // SQL Query  solutionId check 
            string sql = @"
        SELECT d.*, ep.Platform_Name AS SolutionName, e.Emp_Name AS CreatedByName
        FROM Document d
        LEFT JOIN external_platforms ep ON d.Solution_ID = ep.ID
        LEFT JOIN employee e ON d.Created_By = e.Emp_ID
        WHERE d.Platform_ID = @platformId 
          AND (@solutionId IS NULL OR d.Solution_ID = @solutionId) -- New Filter
          AND (d.Doc_Name LIKE @search OR ep.Platform_Name LIKE @search OR e.Emp_Name LIKE @search)
        ORDER BY d.Created_Time DESC
        LIMIT @offset, @pageSize";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@platformId", externalPlatformId);
            cmd.Parameters.AddWithValue("@solutionId", solutionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                // (Data Mapping)
                int solutionNameCol = r.GetOrdinal("SolutionName");
                int createdByNameCol = r.GetOrdinal("CreatedByName");

                var doc = new Document
                {
                    ID = r.GetInt32("ID"),
                    Platform_ID = r.GetInt32("Platform_ID"),
                    Solution_ID = r.IsDBNull(r.GetOrdinal("Solution_ID")) ? null : r.GetInt32("Solution_ID"),
                    Doc_Name = r.GetString("Doc_Name"),
                    Created_Time = r.IsDBNull(r.GetOrdinal("Created_Time")) ? null : r.GetDateTime("Created_Time"),
                    Created_By = r.IsDBNull(r.GetOrdinal("Created_By")) ? null : r.GetInt32("Created_By"),
                    Doc_Classification = r.IsDBNull(r.GetOrdinal("Doc_Classification")) ? null : r.GetString("Doc_Classification"),
                    Tags = r.IsDBNull(r.GetOrdinal("Tags")) ? null : r.GetString("Tags"),
                    Confidential = r.GetBoolean("Confidential"),
                    Doc_URL = r.IsDBNull(r.GetOrdinal("Doc_URL")) ? null : r.GetString("Doc_URL"),
                    SolutionName = r.IsDBNull(solutionNameCol) ? "None" : r.GetString(solutionNameCol),
                    CreatedByName = r.IsDBNull(createdByNameCol) ? "Unknown" : r.GetString(createdByNameCol)
                };
                list.Add(doc);
            }
            return list;
        }

        // 2. Adding 'int? solutionId' to the GetExternalDocumentCount Method
        public int GetExternalDocumentCount(string search, int? solutionId)
        {
            using var conn = GetConnection();
            conn.Open();

            var externalPlatformId = GetExternalPlatformId();

            string sql = @"
        SELECT COUNT(*) 
        FROM Document d
        LEFT JOIN external_platforms ep ON d.Solution_ID = ep.ID
        LEFT JOIN employee e ON d.Created_By = e.Emp_ID
        WHERE d.Platform_ID = @platformId 
          AND (@solutionId IS NULL OR d.Solution_ID = @solutionId) -- New Filter
          AND (d.Doc_Name LIKE @search OR ep.Platform_Name LIKE @search OR e.Emp_Name LIKE @search)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@platformId", externalPlatformId);
            cmd.Parameters.AddWithValue("@solutionId", solutionId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int GetExternalDocumentCount(string search)
        {
            using var conn = GetConnection();
            conn.Open();

            // Get the correct platform ID for external documents
            var externalPlatformId = GetExternalPlatformId();

            string sql = @"
                SELECT COUNT(*) 
                FROM Document d
                LEFT JOIN external_platforms ep ON d.Solution_ID = ep.ID
                LEFT JOIN employee e ON d.Created_By = e.Emp_ID
                WHERE d.Platform_ID = @platformId 
                  AND (d.Doc_Name LIKE @search OR ep.Platform_Name LIKE @search OR e.Emp_Name LIKE @search)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@platformId", externalPlatformId);
            cmd.Parameters.AddWithValue("@search", $"%{search}%");
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public Document? GetById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                SELECT d.*, ip.App_Name AS SolutionName, e.Emp_Name AS CreatedByName
                FROM Document d
                LEFT JOIN Internal_Platforms ip ON d.Solution_ID = ip.ID
                LEFT JOIN external_platforms ep ON d.Solution_ID = ep.ID
                LEFT JOIN employee e ON d.Created_By = e.Emp_ID
                WHERE d.ID = @id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();

            if (r.Read())
            {
                int solutionNameCol = r.GetOrdinal("SolutionName");
                int createdByNameCol = r.GetOrdinal("CreatedByName");

                return new Document
                {
                    ID = r.GetInt32("ID"),
                    Platform_ID = r.GetInt32("Platform_ID"),
                    Solution_ID = r.IsDBNull(r.GetOrdinal("Solution_ID")) ? null : r.GetInt32("Solution_ID"),
                    Doc_Name = r.GetString("Doc_Name"),
                    Created_Time = r.IsDBNull(r.GetOrdinal("Created_Time")) ? null : r.GetDateTime("Created_Time"),
                    Created_By = r.IsDBNull(r.GetOrdinal("Created_By")) ? null : r.GetInt32("Created_By"),
                    Doc_Classification = r.IsDBNull(r.GetOrdinal("Doc_Classification")) ? null : r.GetString("Doc_Classification"),
                    Tags = r.IsDBNull(r.GetOrdinal("Tags")) ? null : r.GetString("Tags"),
                    Confidential = r.GetBoolean("Confidential"),
                    Doc_URL = r.IsDBNull(r.GetOrdinal("Doc_URL")) ? null : r.GetString("Doc_URL"),
                    SolutionName = r.IsDBNull(solutionNameCol) ? "None" : r.GetString(solutionNameCol),
                    CreatedByName = r.IsDBNull(createdByNameCol) ? "Unknown" : r.GetString(createdByNameCol)
                };
            }
            return null;
        }

        public void Insert(Document doc)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                INSERT INTO Document 
                (Platform_ID, Solution_ID, Doc_Name, Created_Time, Created_By, 
                 Doc_Classification, Tags, Confidential, Doc_URL)
                VALUES 
                (@Platform_ID, @Solution_ID, @Doc_Name, @Created_Time, @Created_By, 
                 @Doc_Classification, @Tags, @Confidential, @Doc_URL)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Platform_ID", doc.Platform_ID);
            cmd.Parameters.AddWithValue("@Solution_ID", doc.Solution_ID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Doc_Name", doc.Doc_Name);
            cmd.Parameters.AddWithValue("@Created_Time", doc.Created_Time ?? DateTime.Now);
            cmd.Parameters.AddWithValue("@Created_By", doc.Created_By ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Doc_Classification", doc.Doc_Classification ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", doc.Tags ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidential", doc.Confidential);
            cmd.Parameters.AddWithValue("@Doc_URL", doc.Doc_URL ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void Update(Document doc)
        {
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                UPDATE Document SET 
                    Solution_ID = @Solution_ID,
                    Doc_Name = @Doc_Name,
                    Doc_Classification = @Doc_Classification,
                    Tags = @Tags,
                    Confidential = @Confidential,
                    Doc_URL = @Doc_URL
                WHERE ID = @ID";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ID", doc.ID);
            cmd.Parameters.AddWithValue("@Solution_ID", doc.Solution_ID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Doc_Name", doc.Doc_Name);
            cmd.Parameters.AddWithValue("@Doc_Classification", doc.Doc_Classification ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Tags", doc.Tags ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Confidential", doc.Confidential);
            cmd.Parameters.AddWithValue("@Doc_URL", doc.Doc_URL ?? (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM Document WHERE ID = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }
    }
}