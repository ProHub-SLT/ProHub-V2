namespace ProHub.Models
{
    public class YearlyRevenue
    {
        public int Project_Year { get; set; }          // Year of the project
        public decimal Total_OTC { get; set; }         // One-time charge / project value
        public decimal Total_MRC { get; set; }         // Recurring MRC / maintenance effort value
        public decimal MRC_For_StartYear { get; set; } // Used in actual revenue calculation
        public decimal Grand_Total { get; set; }       // Total of all values
    }
}
