using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class ParentProject
    {
      
        public int ParentProjectID { get; set; }
   
        public string? ParentProjectGroup { get; set; }
        
        public string? OperationScope { get; set; }


        // Navigation property for InternalPlatform
        [ValidateNever]
        public ICollection<InternalPlatform> InternalPlatforms { get; set; }
    }
}
