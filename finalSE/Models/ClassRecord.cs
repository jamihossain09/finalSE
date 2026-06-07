using System;
using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class ClassRecord
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string UploadType { get; set; } // "File" or "Link"
        
        public string FilePathOrLink { get; set; }
        
        public int TeacherId { get; set; }
        public virtual Teacher? Teacher { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
