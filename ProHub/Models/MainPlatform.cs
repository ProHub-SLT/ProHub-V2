// File: Models/MainPlatform.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProHub.Models
{
    [Table("Main_Platforms")]
    public class MainPlatform
    {
        [Key]
        public int ID { get; set; }

        public string? Platforms { get; set; }
    }
}