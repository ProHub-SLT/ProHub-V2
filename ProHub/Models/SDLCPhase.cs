using System.Collections.Generic;

namespace ProHub.Models
{
    public class SDLCPhase
    {
        public int Id { get; set; }
        public string Phase { get; set; }
        public int? OrderSeq { get; set; }

        // Navigation property
        public ICollection<ExternalPlatform> ExternalPlatforms { get; set; }

        // Navigation property for InternalPlatform
        public ICollection<InternalPlatform> InternalPlatforms { get; set; }
    }
}
