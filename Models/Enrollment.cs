using System.ComponentModel.DataAnnotations;

namespace SmartAcademyApi.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }

        //(StudentID)
        public int StudentId { get; set; }
        public User Student { get; set; }

        // معرف الكورس المسجل فيه
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;


    }
}
