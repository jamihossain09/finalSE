using System;
using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class Routine
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Type { get; set; } // Class / Exam
        
        [Required]
        public string FilePath { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public int? DepartmentId { get; set; }
        public virtual DepartmentModel? Department { get; set; }
    }
}