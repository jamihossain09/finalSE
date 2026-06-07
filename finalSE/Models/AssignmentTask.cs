using System;
using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class AssignmentTask
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        
        public string? FilePath { get; set; } // Optional attachment file path
        
        public int TeacherId { get; set; }
        public virtual Teacher? Teacher { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
