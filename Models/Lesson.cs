

using System.ComponentModel.DataAnnotations;

namespace SmartAcademyApi.Models
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string VideoUrl { get; set; }

        // المفتاح الأجنبي المرتبط بالكورس
        public int CourseId { get; set; }
        public Course Course { get; set; }


    }
}
