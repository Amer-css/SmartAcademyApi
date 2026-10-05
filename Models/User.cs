using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;


namespace SmartAcademyApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // Admin, Instructor, Student

        // علاقة One-to-Many: المدرس الواحد يمكن أن يمتلك عدة كورسات
        public ICollection<Course> Courses { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }



        public string RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenRevokedAt { get; set; }
    }
}
