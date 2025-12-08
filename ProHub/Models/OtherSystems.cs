using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProHub.Models
{
    [Table("Other_Systems")]
    public class OtherSystems
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OSystem_ID { get; set; }

        [Required]
        [StringLength(255)]
        public string OSystem_Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Developed_Team { get; set; }

        [StringLength(255)]
        public string? Business_Owner { get; set; }

        [StringLength(50)]
        public string? OSystem_IP { get; set; }

        [StringLength(500)]
        public string? OSystem_URL { get; set; }

        public string? OSystem_Users { get; set; } // TEXT maps to string in C#
    }
}