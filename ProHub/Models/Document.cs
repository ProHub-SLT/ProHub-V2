// File: Models/Document.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class Document
    {
        public int ID { get; set; }
        public int Platform_ID { get; set; } = 1;
        
        [Required(ErrorMessage = "Solution is required")]
        public int? Solution_ID { get; set; }
        public string Doc_Name { get; set; } = "";
        public DateTime? Created_Time { get; set; }
        public int? Created_By { get; set; }
        public string? Doc_Type { get; set; }
        public string? Doc_Classification { get; set; }
        public string? Doc_URL { get; set; }
        public string? Tags { get; set; }
        public bool Confidential { get; set; } = false;

        // View Helpers
        public string? SolutionName { get; set; }
        public string? CreatedByName { get; set; }
    }
}