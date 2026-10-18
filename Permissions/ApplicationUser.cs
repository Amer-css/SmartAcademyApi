namespace SmartAcademyApi.Permissions
{
    public class ApplicationUser
    {
        public string Id { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}
