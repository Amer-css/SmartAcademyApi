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
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;


        // One-to-Many relationship: A single teacher can have multiple courses.
        public ICollection<Course> Courses { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }

        public bool IsActive { get; set; } = true;


        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenRevokedAt { get; set; }
    }
}
