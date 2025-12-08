namespace ProHub.Models
{
    public class TargetEndUser
    {
        public int ID { get; set; }
        public string EndUserType { get; set; }



        // Navigation property for InternalPlatform
        public ICollection<InternalPlatform> InternalPlatforms { get; set; }
    }
}
