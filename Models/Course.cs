using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SmartAcademyApi.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // The foreign key linking the course to the instructor
        public int InstructorId { get; set; }

        public bool IsActive { get; set; } = true; // or IsDeleted = false

        [ForeignKey("InstructorId")]
        public User Instructor { get; set; } // Teacher Mobility Relationship

        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<Exam> Exams { get; set; }

    }
}
