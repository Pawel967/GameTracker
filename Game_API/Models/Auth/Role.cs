namespace Game_API.Models.Auth
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<User> Users { get; set; } = new();

        public static class RoleNames
        {
            public const string Admin = "Admin";
            public const string User = "User";
        }
    }
}
