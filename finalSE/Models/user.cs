using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        // This property is named PasswordHash in your view; adjust if you store a plain password elsewhere.
        [Required]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        public string Address { get; set; }
        public int RoleId { get; set; }

        public Role Role { get; set; }

        public DateTime? LastLogin { get; set; }

        // 🔥 ADD THIS
        public DateTime CreatedAt { get; set; }
    }
}
