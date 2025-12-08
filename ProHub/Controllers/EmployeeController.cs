using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using ProHub.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public class EmployeeController : Controller
{
    private readonly string _connectionString;

    public EmployeeController(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection");
    }

    // -------------------------------------------------------
    // LOAD DROPDOWN GROUPS
    // -------------------------------------------------------
    private List<EmpGroup> GetGroups()
    {
        List<EmpGroup> groups = new List<EmpGroup>();

        using (var con = new MySqlConnection(_connectionString))
        {
            con.Open();
            string sql = "SELECT GroupID, GroupName FROM empgroup";

            using (var cmd = new MySqlCommand(sql, con))
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    groups.Add(new EmpGroup
                    {
                        GroupID = dr.GetInt32("GroupID"),
                        GroupName = dr.GetString("GroupName")
                    });
                }
            }
        }
        return groups;
    }

    // -------------------------------------------------------
    // CREATE: GET
    // -------------------------------------------------------
    public IActionResult Create()
    {
        ViewBag.Groups = GetGroups();
        return View();
    }

    // -------------------------------------------------------
    // CREATE: POST
    // -------------------------------------------------------
    [HttpPost]
    public IActionResult Create(Employee emp)
    {
        using (var con = new MySqlConnection(_connectionString))
        {
            con.Open();

            string sql = @"
                INSERT INTO employee
                (Emp_Id, Emp_Name, Emp_Email, Emp_Phone, GroupID, DOB, Calling_Name, Gender, Section)
                VALUES
                (@EmpId, @EmpName, @EmpEmail, @EmpPhone, @GroupID, @DOB, @CallingName, @Gender, @Section)";

            using (var cmd = new MySqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpId", emp.EmpId);
                cmd.Parameters.AddWithValue("@EmpName", emp.EmpName);
                cmd.Parameters.AddWithValue("@EmpEmail", emp.EmpEmail);
                cmd.Parameters.AddWithValue("@EmpPhone", emp.EmpPhone);
                cmd.Parameters.AddWithValue("@GroupID", emp.GroupID);
                cmd.Parameters.AddWithValue("@DOB", emp.DOB);
                cmd.Parameters.AddWithValue("@CallingName", emp.CallingName);
                cmd.Parameters.AddWithValue("@Gender", emp.Gender);
                cmd.Parameters.AddWithValue("@Section", emp.Section);

                cmd.ExecuteNonQuery();
            }
        }

        TempData["SuccessMessage"] = "Contact Added Successfully!";
        return RedirectToAction("Create");
    }

    // -------------------------------------------------------
    // INDEX: SHOW ALL EMPLOYEES
    // -------------------------------------------------------
    public IActionResult Index(
        string search = "",
        string sortColumn = "EmpName",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 10)
    {
        return LoadEmployees(search, null, sortColumn, sortOrder, page, pageSize);
    }

    // -------------------------------------------------------
    // DIVISIONAL MEMBERS
    // -------------------------------------------------------
    public IActionResult DivisionalMembers(
        string search = "",
        string sortColumn = "EmpName",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 10)
    {
        // Multiple group filter
        string filter = "Administrator,Developer,Non Developer,Ishamp Users,DPO";

        return LoadEmployees(search, filter, sortColumn, sortOrder, page, pageSize);
    }

    // -------------------------------------------------------
    // VIEW ONLY USERS
    // -------------------------------------------------------
    public IActionResult ViewOnly(
        string search = "",
        string sortColumn = "EmpName",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 10)
    {
        return LoadEmployees(search, "View Only User", sortColumn, sortOrder, page, pageSize);
    }

    // -------------------------------------------------------
    // SHARED METHOD TO LOAD EMPLOYEE LIST
    // -------------------------------------------------------
    private IActionResult LoadEmployees(
        string search,
        string groupFilter,
        string sortColumn = "EmpName",
        string sortOrder = "asc",
        int page = 1,
        int pageSize = 10)
    {
        List<Employee> employees = new List<Employee>();

        using (var con = new MySqlConnection(_connectionString))
        {
            con.Open();

            string sql = @"
                SELECT e.Emp_Id, e.Emp_Name, e.Emp_Email, e.Emp_Phone, g.GroupName
                FROM employee e
                INNER JOIN empgroup g ON e.GroupID = g.GroupID
                WHERE (@search IS NULL
                       OR e.Emp_Name LIKE CONCAT('%', @search, '%')
                       OR e.Emp_Email LIKE CONCAT('%', @search, '%')
                       OR e.Emp_Phone LIKE CONCAT('%', @search, '%'))
                  AND (@filter IS NULL 
                       OR (@filter IS NOT NULL AND FIND_IN_SET(g.GroupName, @filter)))";

            using (var cmd = new MySqlCommand(sql, con))
            {
                // FIXED: search NULL handling
                cmd.Parameters.AddWithValue("@search",
                    string.IsNullOrWhiteSpace(search) ? null : search);

                cmd.Parameters.AddWithValue("@filter",
                    string.IsNullOrWhiteSpace(groupFilter) ? null : groupFilter);

                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        employees.Add(new Employee
                        {
                            EmpId = dr.GetInt32("Emp_Id"),
                            EmpName = dr.GetString("Emp_Name"),
                            EmpEmail = dr.GetString("Emp_Email"),
                            EmpPhone = dr.GetString("Emp_Phone")
                        });
                    }
                }
            }
        }

        // Sorting
        employees = sortColumn switch
        {
            "EmpName" => sortOrder == "asc"
                ? employees.OrderBy(x => x.EmpName).ToList()
                : employees.OrderByDescending(x => x.EmpName).ToList(),

            "EmpEmail" => sortOrder == "asc"
                ? employees.OrderBy(x => x.EmpEmail).ToList()
                : employees.OrderByDescending(x => x.EmpEmail).ToList(),

            "EmpPhone" => sortOrder == "asc"
                ? employees.OrderBy(x => x.EmpPhone).ToList()
                : employees.OrderByDescending(x => x.EmpPhone).ToList(),

            _ => employees.OrderBy(x => x.EmpName).ToList()
        };

        // Pagination
        int totalRecords = employees.Count;
        var paginatedList = employees
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // ViewBag
        ViewBag.Search = search;
        ViewBag.GroupFilter = groupFilter;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.SortColumn = sortColumn;
        ViewBag.SortOrder = sortOrder;

        return View("Index", paginatedList);
    }

    // -------------------------------------------------------
    // EDIT: GET
    // -------------------------------------------------------
    public IActionResult Edit(int id)
    {
        Employee emp = new Employee();

        using (var con = new MySqlConnection(_connectionString))
        {
            con.Open();
            string sql = "SELECT * FROM employee WHERE Emp_Id=@id";

            using (var cmd = new MySqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@id", id);

                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        emp.EmpId = dr.GetInt32("Emp_Id");
                        emp.EmpName = dr.GetString("Emp_Name");
                        emp.EmpEmail = dr.GetString("Emp_Email");
                        emp.EmpPhone = dr.GetString("Emp_Phone");
                        emp.GroupID = dr.GetInt32("GroupID");
                        emp.CallingName = dr.GetString("Calling_Name");
                        emp.Section = dr.GetString("Section");
                        emp.Gender = dr.GetString("Gender");

                        // FIXED - NULL SAFE DOB READING
                        int dobIndex = dr.GetOrdinal("DOB");
                        emp.DOB = dr.IsDBNull(dobIndex) ? (DateTime?)null : dr.GetDateTime(dobIndex);
                    }
                }
            }
        }

        ViewBag.Groups = GetGroups();
        return View(emp);
    }

    // -------------------------------------------------------
    // EDIT: POST – UPDATE EMPLOYEE
    // -------------------------------------------------------
    [HttpPost]
    public IActionResult Edit(Employee emp)
    {
        using (var con = new MySqlConnection(_connectionString))
        {
            con.Open();

            string sql = @"
                UPDATE employee SET
                    Emp_Name=@EmpName,
                    Emp_Email=@EmpEmail,
                    Emp_Phone=@EmpPhone,
                    GroupID=@GroupID,
                    DOB=@DOB,
                    Calling_Name=@CallingName,
                    Gender=@Gender,
                    Section=@Section
                WHERE Emp_Id=@EmpId";

            using (var cmd = new MySqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@EmpId", emp.EmpId);
                cmd.Parameters.AddWithValue("@EmpName", emp.EmpName);
                cmd.Parameters.AddWithValue("@EmpEmail", emp.EmpEmail);
                cmd.Parameters.AddWithValue("@EmpPhone", emp.EmpPhone);
                cmd.Parameters.AddWithValue("@GroupID", emp.GroupID);
                cmd.Parameters.AddWithValue("@DOB", emp.DOB);
                cmd.Parameters.AddWithValue("@CallingName", emp.CallingName);
                cmd.Parameters.AddWithValue("@Gender", emp.Gender);
                cmd.Parameters.AddWithValue("@Section", emp.Section);

                cmd.ExecuteNonQuery();
            }
        }

        TempData["SuccessMessage"] = "Employee updated successfully!";
        return RedirectToAction("Edit", new { id = emp.EmpId });
    }
}
