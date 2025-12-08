namespace ProHub.Models
{
    public class ProjectRevenue
    {
        public string Project_Name { get; set; }      // Project or platform name
        public string Developed_By { get; set; }      // For internal projects
        public int Start_Year { get; set; }           // Launch year
        public int Start_Month { get; set; }          // Launch month
        public decimal MRC { get; set; }              // Monthly recurring charge
        public decimal MRC_For_Year { get; set; }     // Calculated MRC for selected year
        public decimal MRC_For_StartYear { get; set; }// MRC for start year
        public decimal OTC { get; set; }              // One-time charge
        public int Contract_Period { get; set; }      // Contract period in years (external projects)
        public int Months_To_Current { get; set; }    // Months considered in calculation
    }
}
