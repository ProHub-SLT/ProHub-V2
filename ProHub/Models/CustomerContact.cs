using System.ComponentModel.DataAnnotations;

namespace ProHub.Models
{
    public class CustomerContact
    {
        public int ID { get; set; }

       
        public int? Platform_ID { get; set; }

        
        public string? Customer_Title { get; set; }

        
        public string? Contact_Name { get; set; }

        public string? Contact_Phone1 { get; set; }

      
        public string? Contact_Phone2 { get; set; }

      
        public string? Contact_Email { get; set; }

       
        public string? Contact_Designation { get; set; }

      
        public int? Contact_Company { get; set; }

       
        // Navigation properties (optional)
        public ExternalPlatform? Platform { get; set; }
        public Company? Company { get; set; }
    }
}
