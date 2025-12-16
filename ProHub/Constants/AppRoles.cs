// Constants/AppRoles.cs
namespace ProHub.Constants // or just ProHub if you prefer
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Developer = "Developer";
        public const string ViewOnly = "ViewOnly";

        public const string DPO = "DPO";           // Data Protection Officer
        public const string Ishamp = "Ishamp";        // Ishamp Users
        public const string Restricted = "Restricted";    // Non Developer, etc.

        // Blocked completely
        public const string Inactive = "Inactive";
    }
}