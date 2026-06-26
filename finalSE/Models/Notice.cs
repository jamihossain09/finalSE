using System;
using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class Notice
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string FilePath { get; set; } // PDF file path
        
        public DateTime PublishedAt { get; set; } = DateTime.Now;

        // Department wise
        public int? DepartmentId { get; set; }
        public virtual DepartmentModel? Department { get; set; }
    }
}
