using System;

namespace ProHub.Models
{
    public class Partner
    {
        public int ID { get; set; }
        public string Partner_Organization { get; set; }
        public string Partner_Title { get; set; }
        public string Partner_Name { get; set; }
        public string Partner_Phone1 { get; set; }
        public string? Partner_Phone2 { get; set; }
        public string Partner_Email { get; set; }
        public string? Partner_Designation { get; set; }
    }
}
