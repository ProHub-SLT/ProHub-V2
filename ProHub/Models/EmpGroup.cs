using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;

namespace ProHub.Models
{
    public class EmpGroup
    {
        public int GroupID { get; set; }
        public string GroupName { get; set; }

        // Navigation property
        [ValidateNever]
        public ICollection<Employee> Employees { get; set; }
    }
}
