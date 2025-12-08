// Models/InternalProjectComment.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace ProHub.Models
{
    public class InternalProjectComment
    {
        public int ID { get; set; }
        public int Solution_ID { get; set; }
        public string Comment { get; set; }
        public int? Updated_By { get; set; }
        public DateTime? Updated_Time { get; set; }
        
        // Navigation properties
        public InternalPlatform? InternalPlatform { get; set; }
        public Employee? UpdatedByEmployee { get; set; }
    }
}