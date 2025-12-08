using System.Collections.Generic;

namespace ProHub.Models
{
    public class TestPermissionsViewModel
    {
        public string EmployeeId { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public List<(string Type, string Value)> Claims { get; set; }
    }
}
