namespace SmartAcademyApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Admin, Student

        //A single person uses multiple user accounts
        public ICollection<User> Users { get; set; } = new List<User>();

        // A single role possesses multiple permissions.
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
