using MySql.Data.MySqlClient;
using ProHub.Models;

namespace ProHub.Data
{
    public class ConsumerPlatformRepository
    {
        private readonly IConfiguration _configuration;

        public ConsumerPlatformRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private MySqlConnection GetConnection()
        {
            return new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // Helper method to safely get values from reader with proper type conversion
        private T GetValueOrDefault<T>(MySqlDataReader reader, string columnName, T defaultValue = default)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return defaultValue;

                var value = reader.GetValue(ordinal);

                // Handle nullable decimals and numeric types safely
                if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                    return (T)(object)Convert.ToDecimal(value);

                if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                    return (T)(object)Convert.ToInt32(value);

                if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                    return (T)(object)Convert.ToDouble(value);

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public List<InternalPlatform> GetConsumerPlatform(string search = "")
        {
            var list = new List<InternalPlatform>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
            SELECT 
                 ip.id AS Id,
                 ip.app_name AS AppName,
                 ip.app_category AS AppCategory,
                 ip.app_url AS AppURL,
                 ip.app_ip AS AppIP,
                 ip.startdate AS StartDate,
                 ip.targetdate AS TargetDate,
                 ip.vadate AS VADate,
                 ip.launcheddate AS LaunchedDate,
                 ip.percentagedone AS PercentageDone,
                 ip.status AS Status,
                 ip.price AS Price,
                 ip.developed_by AS DevelopedById,
                 dev.emp_name AS DevelopedByName,
                 ip.sdlcphase AS SDLCPhaseId,
                 sp.phase AS SDLCPhaseName,
                 ip.endusertype AS EndUserTypeId,
                 e.id AS EndUserId,
                 e.endusertype AS EndUserTypeName,
                 ip.mainappid AS MainAppID,
                 ma.app_name AS MainAppName,
                 pp.parentprojectgroup AS ParentProjectName,
                 ipc.comment AS Comment
            FROM internal_platforms ip
            INNER JOIN targetenduser e ON ip.endusertype = e.id
            LEFT JOIN employee dev ON ip.developed_by = dev.emp_id
            LEFT JOIN sdlcphas sp ON ip.sdlcphase = sp.id
            LEFT JOIN internal_platforms ma ON ip.mainappid = ma.id
            LEFT JOIN parentproject pp ON ip.parentprojectid = pp.parentprojectid
            LEFT JOIN (
                 SELECT solution_id, comment 
                 FROM internal_project_comments 
                 WHERE id IN (
                     SELECT MAX(id) 
                     FROM internal_project_comments 
                     GROUP BY solution_id
                 )
            ) ipc ON ip.id = ipc.solution_id
            WHERE e.endusertype = 'SLT Employees'
            ";

                        if (!string.IsNullOrEmpty(search))
                        {
                            query += @" AND (
                    ip.app_name LIKE @search OR
                    dev.emp_name LIKE @search OR
                    e.endusertype LIKE @search
                )";
            }


            using var cmd = new MySqlCommand(query, conn);

            if (!string.IsNullOrEmpty(search))
            {
                cmd.Parameters.AddWithValue("@search", $"%{search}%");
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new InternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    AppName = GetValueOrDefault(reader, "AppName", ""),
                    AppCategory = GetValueOrDefault(reader, "AppCategory", ""),
                    AppURL = GetValueOrDefault(reader, "AppURL", ""),
                    AppIP = GetValueOrDefault(reader, "AppIP", ""),
                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? (DateTime?)null : reader.GetDateTime("StartDate"),
                    TargetDate = reader.IsDBNull(reader.GetOrdinal("TargetDate")) ? (DateTime?)null : reader.GetDateTime("TargetDate"),
                    VADate = reader.IsDBNull(reader.GetOrdinal("VADate")) ? (DateTime?)null : reader.GetDateTime("VADate"),
                    LaunchedDate = reader.IsDBNull(reader.GetOrdinal("LaunchedDate")) ? (DateTime?)null : reader.GetDateTime("LaunchedDate"),
                    PercentageDone = GetValueOrDefault(reader, "PercentageDone", (decimal?)null),
                    Status = GetValueOrDefault(reader, "Status", ""),
                    Price = GetValueOrDefault(reader, "Price", (decimal?)null),

                    // Comment field mapped to DPOHandoverComment or a specific comment property
                    DPOHandoverComment = GetValueOrDefault(reader, "Comment", ""),

                    DevelopedById = GetValueOrDefault(reader, "DevelopedById", (int?)null),
                    DevelopedBy = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "DevelopedById", 0),
                        EmpName = GetValueOrDefault(reader, "DevelopedByName", "")
                    },
                    SDLCPhaseId = GetValueOrDefault(reader, "SDLCPhaseId", (int?)null),
                    SDLCPhase = new SDLCPhase
                    {
                        Id = GetValueOrDefault(reader, "SDLCPhaseId", 0),
                        Phase = GetValueOrDefault(reader, "SDLCPhaseName", "")
                    },
                    EndUserTypeId = GetValueOrDefault(reader, "EndUserTypeId", (int?)null),
                    EndUserType = new TargetEndUser
                    {
                        ID = GetValueOrDefault(reader, "EndUserId", 0),
                        EndUserType = GetValueOrDefault(reader, "EndUserTypeName", "")
                    },
                    MainAppID = GetValueOrDefault(reader, "MainAppID", (int?)null),
                    MainAppName = GetValueOrDefault(reader, "MainAppName", ""),
                    ParentProject = new ParentProject
                    {
                        ParentProjectGroup = GetValueOrDefault(reader, "ParentProjectName", "")
                    }
                });
            }

            return list;
        }

        // ✅ Get a single consumer platform record by ID
        public InternalPlatform GetConsumerPlatformById(int id)
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                     ip.id AS Id,
                     ip.app_name AS AppName,
                     ip.developed_by AS DevelopedById,
                     e1.emp_id AS DevelopedByEmpId,
                     e1.emp_name AS DevelopedByName,
                     e1.emp_email AS DevelopedByEmail,
                     e1.emp_phone AS DevelopedByPhone,
                     ip.developed_team AS DevelopedTeam,
                     ip.startdate AS StartDate,
                     ip.targetdate AS TargetDate,
                     ip.bitbucket AS BitBucket,
                     ip.bit_bucket_repo AS BitBucketRepo,
                     ip.sdlcphase AS SDLCPhaseId,
                     sp.id AS SDLCPhaseSDLCId,
                     sp.phase AS SDLCPhaseName,
                     sp.orderseq AS SDLCOrderSeq,
                     ip.percentagedone AS PercentageDone,
                     ip.status AS Status,
                     ip.statusdate AS StatusDate,
                     ip.bus_owner AS BusOwner,
                     ip.app_category AS AppCategory,
                     ip.scope AS Scope,
                     ip.app_ip AS AppIP,
                     ip.app_url AS AppURL,
                     ip.app_users AS AppUsers,
                     ip.uatdate AS UATDate,
                     ip.integrated_apps AS IntegratedApps,
                     ip.dr AS DR,
                     ip.launcheddate AS LaunchedDate,
                     ip.vadate AS VADate,
                     ip.waf AS WAF,
                     ip.app_op_owner AS APPOwner,
                     ip.app_business_owner AS AppBusinessOwner,
                     ip.price AS Price,
                     ip.endusertype AS EndUserTypeId,
                     e.id AS EndUserId,
                     e.endusertype AS EndUserTypeName,
                     ip.requestno AS RequestNo,
                     ip.parentprojectid AS ParentProjectID,
                     pp.parentprojectid AS ParentProjectParentId,
                     pp.parentprojectgroup AS ParentProjectName,  
                     ip.sla AS SLA,
                     ip.backupofficer_1 AS BackupOfficer1Id,
                     e2.emp_id AS Backup1EmpId,
                     e2.emp_name AS Backup1Name,
                     e2.emp_email AS Backup1Email,
                     ip.backupofficer_2 AS BackupOfficer2Id,
                     e3.emp_id AS Backup2EmpId,
                     e3.emp_name AS Backup2Name,
                     e3.emp_email AS Backup2Email,
                     ip.mainappid AS MainAppID,
                     ma.app_name AS MainAppName,   
                     ma.app_url AS MainAppURL,     
                     ip.sslcertificateexpdate AS SSLCertificateExpDate
                FROM internal_platforms ip
                INNER JOIN targetenduser e ON ip.endusertype = e.id
                LEFT JOIN employee e1 ON ip.developed_by = e1.emp_id
                LEFT JOIN employee e2 ON ip.backupofficer_1 = e2.emp_id
                LEFT JOIN employee e3 ON ip.backupofficer_2 = e3.emp_id
                LEFT JOIN sdlcphas sp ON ip.sdlcphase = sp.id
                LEFT JOIN parentproject pp ON ip.parentprojectid = pp.parentprojectid
                LEFT JOIN internal_platforms ma ON ip.mainappid = ma.id  
                WHERE ip.id = @id 
                AND e.endusertype = 'SLT Employees'
                ";


            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var platform = new InternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    AppName = GetValueOrDefault(reader, "AppName", ""),
                    DevelopedById = GetValueOrDefault(reader, "DevelopedById", (int?)null),
                    DevelopedBy = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "DevelopedByEmpId", 0),
                        EmpName = GetValueOrDefault(reader, "DevelopedByName", ""),
                        EmpEmail = GetValueOrDefault(reader, "DevelopedByEmail", ""),
                        EmpPhone = GetValueOrDefault(reader, "DevelopedByPhone", "")
                    },
                    DevelopedTeam = GetValueOrDefault(reader, "DevelopedTeam", ""),
                    BitBucket = GetValueOrDefault(reader, "BitBucket", ""),
                    BitBucketRepo = GetValueOrDefault(reader, "BitBucketRepo", ""),
                    SDLCPhaseId = GetValueOrDefault(reader, "SDLCPhaseId", (int?)null),
                    SDLCPhase = new SDLCPhase
                    {
                        Id = GetValueOrDefault(reader, "SDLCPhaseSDLCId", 0),
                        Phase = GetValueOrDefault(reader, "SDLCPhaseName", ""),
                        OrderSeq = GetValueOrDefault(reader, "SDLCOrderSeq", 0)
                    },
                    StartDate = reader.IsDBNull(reader.GetOrdinal("StartDate")) ? (DateTime?)null : reader.GetDateTime("StartDate"),
                    TargetDate = reader.IsDBNull(reader.GetOrdinal("TargetDate")) ? (DateTime?)null : reader.GetDateTime("TargetDate"),
                    PercentageDone = GetValueOrDefault(reader, "PercentageDone", (decimal?)null),
                    Status = GetValueOrDefault(reader, "Status", ""),
                    StatusDate = reader.IsDBNull(reader.GetOrdinal("StatusDate")) ? (DateTime?)null : reader.GetDateTime("StatusDate"),
                    BusOwner = GetValueOrDefault(reader, "BusOwner", ""),
                    AppCategory = GetValueOrDefault(reader, "AppCategory", ""),
                    Scope = GetValueOrDefault(reader, "Scope", ""),
                    AppIP = GetValueOrDefault(reader, "AppIP", ""),
                    AppURL = GetValueOrDefault(reader, "AppURL", ""),
                    AppUsers = GetValueOrDefault(reader, "AppUsers", ""),
                    UATDate = reader.IsDBNull(reader.GetOrdinal("UATDate")) ? (DateTime?)null : reader.GetDateTime("UATDate"),
                    IntegratedApps = GetValueOrDefault(reader, "IntegratedApps", ""),
                    DR = GetValueOrDefault(reader, "DR", ""),
                    LaunchedDate = reader.IsDBNull(reader.GetOrdinal("LaunchedDate")) ? (DateTime?)null : reader.GetDateTime("LaunchedDate"),
                    VADate = reader.IsDBNull(reader.GetOrdinal("VADate")) ? (DateTime?)null : reader.GetDateTime("VADate"),
                    WAF = GetValueOrDefault(reader, "WAF", ""),
                    APPOwner = GetValueOrDefault(reader, "APPOwner", ""),
                    AppBusinessOwner = GetValueOrDefault(reader, "AppBusinessOwner", ""),
                    Price = GetValueOrDefault(reader, "Price", (decimal?)null),
                    EndUserTypeId = GetValueOrDefault(reader, "EndUserTypeId", (int?)null),
                    EndUserType = new TargetEndUser
                    {
                        ID = GetValueOrDefault(reader, "EndUserId", 0),
                        EndUserType = GetValueOrDefault(reader, "EndUserTypeName", "")
                    },
                    RequestNo = GetValueOrDefault(reader, "RequestNo", ""),
                    ParentProjectID = GetValueOrDefault(reader, "ParentProjectID", (int?)null),
                    ParentProject = new ParentProject
                    {
                        ParentProjectID = GetValueOrDefault(reader, "ParentProjectParentId", 0),
                        ParentProjectGroup = GetValueOrDefault(reader, "ParentProjectName", "")
                    },
                    SLA = GetValueOrDefault(reader, "SLA", ""),
                    BackupOfficer1Id = GetValueOrDefault(reader, "BackupOfficer1Id", (int?)null),
                    BackupOfficer1 = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup1EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup1Name", ""),
                        EmpEmail = GetValueOrDefault(reader, "Backup1Email", "")
                    },
                    BackupOfficer2Id = GetValueOrDefault(reader, "BackupOfficer2Id", (int?)null),
                    BackupOfficer2 = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup2EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup2Name", ""),
                        EmpEmail = GetValueOrDefault(reader, "Backup2Email", "")
                    },
                    MainAppID = GetValueOrDefault(reader, "MainAppID", (int?)null),
                    MainApp = new InternalPlatform
                    {
                        Id = GetValueOrDefault(reader, "MainAppID", 0),
                        AppName = GetValueOrDefault(reader, "MainAppName", ""),
                        AppURL = GetValueOrDefault(reader, "MainAppURL", "")
                    },
                    SSLCertificateExpDate = reader.IsDBNull(reader.GetOrdinal("SSLCertificateExpDate")) ? (DateTime?)null : reader.GetDateTime("SSLCertificateExpDate")
                };

                return platform;
            }

            return null;
        }





        // get all internal platform names
        public List<InternalPlatform> GetAll()
        {
            var list = new List<InternalPlatform>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID, App_Name FROM internal_platforms ORDER BY App_Name", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InternalPlatform { Id = r.GetInt32(0), AppName = r.GetString(1) });
            return list;
        }

        // Get all main platforms
        public List<MainPlatform> GetAllMainPlatforms()
        {
            var list = new List<MainPlatform>();
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID, Platforms FROM main_platforms ORDER BY Platforms", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new MainPlatform { ID = r.GetInt32(0), Platforms = r.IsDBNull(1) ? null : r.GetString(1) });
            return list;
        }

        // Get the ID of the "internal" platform from Main_Platforms table
        // Made more robust to handle variations like "internal solution"
        public int? GetInternalPlatformId()
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT ID FROM main_platforms WHERE LOWER(Platforms) LIKE '%internal%' LIMIT 1", conn);
            using var r = cmd.ExecuteReader();
            if (r.Read())
                return r.GetInt32(0);
            return null;
        }

        public List<InternalPlatform> GetAllInternalPlatformsWithBackupInfo()
        {
            var list = new List<InternalPlatform>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    ip.ID AS Id,
                    ip.App_Name AS AppName,
                    ip.BackupOfficer_1 AS BackupOfficer1Id,
                    e1.Emp_ID AS Backup1EmpId,
                    e1.Emp_Name AS Backup1Name,
                    ip.BackupOfficer_2 AS BackupOfficer2Id,
                    e2.Emp_ID AS Backup2EmpId,
                    e2.Emp_Name AS Backup2Name
                FROM internal_platforms ip
                LEFT JOIN employee e1 ON ip.BackupOfficer_1 = e1.Emp_ID
                LEFT JOIN employee e2 ON ip.BackupOfficer_2 = e2.Emp_ID
                ORDER BY ip.App_Name";

            using var cmd = new MySqlCommand(query, conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var platform = new InternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    AppName = GetValueOrDefault(reader, "AppName", ""),
                    BackupOfficer1Id = GetValueOrDefault(reader, "BackupOfficer1Id", (int?)null),
                    BackupOfficer1 = reader.IsDBNull(reader.GetOrdinal("Backup1EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup1EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup1Name", "")
                    },
                    BackupOfficer2Id = GetValueOrDefault(reader, "BackupOfficer2Id", (int?)null),
                    BackupOfficer2 = reader.IsDBNull(reader.GetOrdinal("Backup2EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup2EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup2Name", "")
                    }
                };

                list.Add(platform);
            }

            return list;
        }


        public List<InternalPlatform> GetRetiredSolutions(string search = "")
        {
            var list = new List<InternalPlatform>();
            using var conn = GetConnection();
            conn.Open();

            string query = @"
       SELECT 
    ip.ID AS Id,
    ip.App_Name AS AppName,
    ip.App_URL AS AppURL,
    ip.App_IP AS AppIP,
    ip.StartDate AS StartDate,
    ip.TargetDate AS TargetDate,
    ip.VADate AS VADate,
    ip.PercentageDone AS PercentageDone,
    ip.Status AS Status,
    ip.LaunchedDate AS LaunchedDate,
    ip.Price AS Price,
    e.Emp_Name AS DevelopedByName,
    sp.Phase AS SDLCPhase,
    pp.ParentProjectGroup AS ParentProjectName,
    ma.App_Name AS MainAppName,
    ipc.Comment AS Comment
FROM internal_platforms ip
LEFT JOIN employee e 
    ON ip.Developed_By = e.Emp_ID
LEFT JOIN sdlcphas sp 
    ON ip.SDLCPhase = sp.ID
LEFT JOIN parentproject pp 
    ON ip.ParentProjectID = pp.ParentProjectID
LEFT JOIN internal_platforms ma 
    ON ip.MainAppID = ma.ID
LEFT JOIN (
    SELECT Solution_ID, Comment
    FROM internal_project_comments
    WHERE ID IN (
        SELECT MAX(ID)
        FROM internal_project_comments
        GROUP BY Solution_ID
    )
) ipc 
    ON ip.ID = ipc.Solution_ID
WHERE sp.Phase = 'Retired';";


            if (!string.IsNullOrEmpty(search))
                query += " AND (ip.App_Name LIKE @search OR e.Emp_Name LIKE @search)";

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrEmpty(search))
                cmd.Parameters.AddWithValue("@search", $"%{search}%");

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new InternalPlatform
                {
                    Id = GetValueOrDefault(r, "Id", 0),
                    AppName = GetValueOrDefault(r, "AppName", ""),
                    MainAppName = GetValueOrDefault(r, "MainAppName", ""),
                    AppURL = GetValueOrDefault(r, "AppURL", ""),
                    AppIP = GetValueOrDefault(r, "AppIP", ""),
                    StartDate = r.IsDBNull(r.GetOrdinal("StartDate")) ? (DateTime?)null : r.GetDateTime("StartDate"),
                    TargetDate = r.IsDBNull(r.GetOrdinal("TargetDate")) ? (DateTime?)null : r.GetDateTime("TargetDate"),
                    VADate = r.IsDBNull(r.GetOrdinal("VADate")) ? (DateTime?)null : r.GetDateTime("VADate"),
                    LaunchedDate = r.IsDBNull(r.GetOrdinal("LaunchedDate")) ? (DateTime?)null : r.GetDateTime("LaunchedDate"),
                    PercentageDone = r.IsDBNull(r.GetOrdinal("PercentageDone")) ? (decimal?)null : r.GetDecimal("PercentageDone"),
                    Status = GetValueOrDefault(r, "Status", ""),
                    Price = GetValueOrDefault(r, "Price", (decimal?)null),
                    DevelopedBy = new Employee
                    {
                        EmpName = GetValueOrDefault(r, "DevelopedByName", "")
                    },
                    SDLCPhase = new SDLCPhase
                    {
                        Phase = GetValueOrDefault(r, "SDLCPhase", "")
                    },
                    ParentProject = new ParentProject
                    {
                        ParentProjectGroup = GetValueOrDefault(r, "ParentProjectName", "")
                    },
                    DPOHandoverComment = GetValueOrDefault(r, "Comment", "")
                });
            }

            return list;
        }

        // New method to get platforms where an employee is a backup officer
        public List<InternalPlatform> GetInternalPlatformsByBackupOfficer(int employeeId)
        {
            var list = new List<InternalPlatform>();

            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT 
                    ip.ID AS Id,
                    ip.App_Name AS AppName,
                    e1.Emp_ID AS DevelopedById,
                    e1.Emp_Name AS DevelopedByName,
                    ip.BackupOfficer_1 AS BackupOfficer1Id,
                    e2.Emp_ID AS Backup1EmpId,
                    e2.Emp_Name AS Backup1Name,
                    ip.BackupOfficer_2 AS BackupOfficer2Id,
                    e3.Emp_ID AS Backup2EmpId,
                    e3.Emp_Name AS Backup2Name
                FROM internal_platforms ip
                LEFT JOIN employee e1 ON ip.Developed_By = e1.Emp_ID
                LEFT JOIN employee e2 ON ip.BackupOfficer_1 = e2.Emp_ID
                LEFT JOIN employee e3 ON ip.BackupOfficer_2 = e3.Emp_ID
                WHERE ip.BackupOfficer_1 = @employeeId OR ip.BackupOfficer_2 = @employeeId
                ORDER BY ip.App_Name";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@employeeId", employeeId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var platform = new InternalPlatform
                {
                    Id = GetValueOrDefault(reader, "Id", 0),
                    AppName = GetValueOrDefault(reader, "AppName", ""),
                    DevelopedBy = new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "DevelopedById", 0),
                        EmpName = GetValueOrDefault(reader, "DevelopedByName", "")
                    },
                    BackupOfficer1Id = GetValueOrDefault(reader, "BackupOfficer1Id", (int?)null),
                    BackupOfficer1 = reader.IsDBNull(reader.GetOrdinal("Backup1EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup1EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup1Name", "")
                    },
                    BackupOfficer2Id = GetValueOrDefault(reader, "BackupOfficer2Id", (int?)null),
                    BackupOfficer2 = reader.IsDBNull(reader.GetOrdinal("Backup2EmpId")) ? null : new Employee
                    {
                        EmpId = GetValueOrDefault(reader, "Backup2EmpId", 0),
                        EmpName = GetValueOrDefault(reader, "Backup2Name", "")
                    }
                };

                list.Add(platform);
            }

            return list;
        }



        public InternalPlatform GetRetiredSolutionById(int id)
        {
            InternalPlatform? item = null;
            using var conn = GetConnection();
            conn.Open();

            string query = @"
        SELECT 
            ip.ID AS Id,
            ip.App_Name AS AppName,
            ip.App_URL AS AppURL,
            ip.App_IP AS AppIP,
            ip.StartDate AS StartDate,
            ip.TargetDate AS TargetDate,
            ip.VADate AS VADate,
            ip.PercentageDone AS PercentageDone,
            ip.Status AS Status,
            ip.LaunchedDate AS LaunchedDate,
            ip.Price AS Price,
            e.Emp_Name AS DevelopedByName,
            sp.Phase AS SDLCPhase,
            pp.ParentProjectGroup AS ParentProjectName,
            ma.App_Name AS MainAppName,
            ipc.Comment AS Comment,
            bo1.Emp_Name AS Backup1Name, 
            bo1.Emp_Email AS Backup1Email,
            bo2.Emp_Name AS Backup2Name, 
            bo2.Emp_Email AS Backup2Email
        FROM internal_platforms ip
        LEFT JOIN employee e 
            ON ip.Developed_By = e.Emp_ID
        LEFT JOIN sdlcphas sp 
            ON ip.SDLCPhase = sp.ID
        LEFT JOIN parentproject pp 
            ON ip.ParentProjectID = pp.ParentProjectID
        LEFT JOIN internal_platforms ma 
            ON ip.MainAppID = ma.ID
        LEFT JOIN employee bo1 
            ON ip.BackupOfficer_1 = bo1.Emp_ID
        LEFT JOIN employee bo2 
            ON ip.BackupOfficer_2 = bo2.Emp_ID
        LEFT JOIN (
            SELECT Solution_ID, Comment
            FROM internal_project_comments
            WHERE ID IN (
                SELECT MAX(ID)
                FROM internal_project_comments
                GROUP BY Solution_ID
            )
        ) ipc 
            ON ip.ID = ipc.Solution_ID
        WHERE ip.ID = @id";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                // Helper function to safely get dates avoiding IsDBNull crashes if column exists
                DateTime? GetDateSafe(string colName)
                {
                    int ord = r.GetOrdinal(colName);
                    return r.IsDBNull(ord) ? (DateTime?)null : r.GetDateTime(ord);
                }

                item = new InternalPlatform
                {
                    Id = GetValueOrDefault(r, "Id", 0),
                    AppName = GetValueOrDefault(r, "AppName", ""),
                    AppURL = GetValueOrDefault(r, "AppURL", ""),
                    AppIP = GetValueOrDefault(r, "AppIP", ""),
                    StartDate = GetDateSafe("StartDate"),
                    TargetDate = GetDateSafe("TargetDate"),
                    VADate = GetDateSafe("VADate"),
                    LaunchedDate = GetDateSafe("LaunchedDate"),
                    PercentageDone = GetValueOrDefault(r, "PercentageDone", (decimal?)null),
                    Status = GetValueOrDefault(r, "Status", ""),
                    Price = GetValueOrDefault(r, "Price", (decimal?)null),

                    // Complex Objects & Display Names
                    DevelopedByName = GetValueOrDefault(r, "DevelopedByName", ""),
                    DevelopedBy = new Employee
                    {
                        EmpName = GetValueOrDefault(r, "DevelopedByName", "")
                    },

                    SDLCPhaseName = GetValueOrDefault(r, "SDLCPhase", ""),
                    SDLCPhase = new SDLCPhase
                    {
                        Phase = GetValueOrDefault(r, "SDLCPhase", "")
                    },

                    ParentProjectGroupName = GetValueOrDefault(r, "ParentProjectName", ""),
                    MainAppName = GetValueOrDefault(r, "MainAppName", ""),
                    Comment = GetValueOrDefault(r, "Comment", ""),

                    // Backup Officers
                    BackupOfficer1 = string.IsNullOrEmpty(GetValueOrDefault(r, "Backup1Name", ""))
                        ? null
                        : new Employee
                        {
                            EmpName = GetValueOrDefault(r, "Backup1Name", ""),
                            EmpEmail = GetValueOrDefault(r, "Backup1Email", "")
                        },
                    BackupOfficer2 = string.IsNullOrEmpty(GetValueOrDefault(r, "Backup2Name", ""))
                        ? null
                        : new Employee
                        {
                            EmpName = GetValueOrDefault(r, "Backup2Name", ""),
                            EmpEmail = GetValueOrDefault(r, "Backup2Email", "")
                        }
                };
            }

            return item!;
        }
    }
}