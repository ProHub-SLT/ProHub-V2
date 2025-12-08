using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;

namespace ProHub.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }

        // Navigation property
        [ValidateNever]
        public ICollection<ExternalPlatform> ExternalPlatforms { get; set; }
    }
}
