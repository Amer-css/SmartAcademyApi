using System.ComponentModel.DataAnnotations;

namespace SmartAcademyApi.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public string Options { get; set; }

        [Required]
        public string CorrectAnswer { get; set; }

        public int ExamId { get; set; }
        public Exam Exam { get; set; }
    }
}
