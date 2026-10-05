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

        // المفتاح الأجنبي الذي يربط الكورس بالمدرس
        public int InstructorId { get; set; }

        [ForeignKey("InstructorId")]
        public User Instructor { get; set; } // علاقة التنقل للمدرس

        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<Lesson> Lessons { get; set; }
        public ICollection<Exam> Exams { get; set; }

    }
}
