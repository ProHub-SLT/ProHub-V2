using System.Collections.Generic;

namespace ProHub.Models
{
    public class SalesTeam
    {
        public int Id { get; set; }
        public string SalesTeamName { get; set; }
        public string SalesTeamGroup { get; set; }
        public string SalesTeamHead { get; set; }

        // Navigation property
        public ICollection<ExternalPlatform> ExternalPlatforms { get; set; }
    }
}
