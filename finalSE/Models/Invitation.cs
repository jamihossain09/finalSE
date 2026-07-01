using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class Invitation
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        // Student or Teacher Role
        public int RoleId { get; set; }

        public virtual Role? Role { get; set; }

        // Unique Invitation Token
        [Required]
        public string Token { get; set; }

        // Link Used or Not
        public bool IsUsed { get; set; } = false;

        // Expire Time
        public DateTime ExpireDate { get; set; }

        // Created Time
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}