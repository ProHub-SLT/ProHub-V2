namespace PROHUB.Models
{
    public class ProjectComment
    {
        public int ID { get; set; }

        public int Activity_ID { get; set; }

        public string Comment { get; set; }

        public int? Updated_By { get; set; }

        public DateTime? Updated_Time { get; set; }

        // ------- Joined Display Fields (Optional but Useful) -------
        public string UpdatedByName { get; set; }  // From Employee table
        public string ActivityDescription { get; set; } // From Project_Activities table
    }
}
