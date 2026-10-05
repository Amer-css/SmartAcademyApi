using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace SmartAcademyApi.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        // الاختبار الواحد يحتوي على عدة أسئلة (One-to-Many)
        public ICollection<Question> Questions { get; set; }

    }
}
