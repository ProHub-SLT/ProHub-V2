using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PROHUB.Models
{
    public class ProjectActivity
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        [Display(Name = "Main Platform")]
        public int PlatformId { get; set; }

        [Display(Name = "Solution")]
        public int? SolutionId { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Created By")]
        public int? CreatedBy { get; set; }

        [Display(Name = "Created Time")]
        public DateTime? CreatedTime { get; set; }

        [Display(Name = "Assigned To")]
        public int? AssignedTo { get; set; }

        [Required(ErrorMessage = "Target Date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Target Date")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? TargetDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string? Status { get; set; }

        [Display(Name = "Updated By")]
        [RequiredOnEdit(ErrorMessage = "Updated By is required when editing")]
        public int? UpdatedBy { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedDate { get; set; }

        // ==========================================
        // DISPLAY / JOINED PROPERTIES (Not in DB Table)
        // ==========================================

        [NotMapped]
        [Display(Name = "Platform Name")]
        public string? PlatformName { get; set; }

        [NotMapped]
        [Display(Name = "Solution Name")]
        public string? SolutionName { get; set; }

        [NotMapped]
        [Display(Name = "Created By")]
        public string? CreatedByName { get; set; }

        [NotMapped]
        [Display(Name = "Assigned To")]
        public string? AssignedToName { get; set; }

        [NotMapped]
        [Display(Name = "Updated By")]
        public string? UpdatedByName { get; set; }

        [NotMapped]
        public string? LatestComment { get; set; }
    }

    // Custom Validation Attribute
    public class RequiredOnEditAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var activity = (ProjectActivity)validationContext.ObjectInstance;

            // If Id > 0, we are Editing. Therefore, validate that UpdatedBy is provided.
            if (activity.Id > 0)
            {
                if (value == null)
                {
                    return new ValidationResult(ErrorMessage ?? "This field is required on edit.");
                }
            }
            
            return ValidationResult.Success;
        }
    }
}