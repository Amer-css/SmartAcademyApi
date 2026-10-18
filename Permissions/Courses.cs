namespace SmartAcademyApi.Permissions
{
    public class Courses
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public string Status { get; set; } // "Published" or "Draft"
    }
}
