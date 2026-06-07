using System;
using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class Tutorial
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        public string? FilePath { get; set; } // PDF or PPT file path
        
        public string? VideoLink { get; set; } // Optional external video link
        
        public int TeacherId { get; set; }
        public virtual Teacher? Teacher { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
