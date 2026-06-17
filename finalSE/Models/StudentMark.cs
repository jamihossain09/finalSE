using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace finalSE.Models
{
    public class StudentMark
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual StudentModel Student { get; set; }

        [Required]
        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }

        [Range(0, 10, ErrorMessage = "Attendance marks must be between 0 and 10.")]
        public double Attendance { get; set; } = 0;

        [Range(0, 20, ErrorMessage = "Class Test marks must be between 0 and 20.")]
        public double ClassTest { get; set; } = 0;

        [Range(0, 30, ErrorMessage = "Mid-term marks must be between 0 and 30.")]
        public double MidTerm { get; set; } = 0;

        [Range(0, 40, ErrorMessage = "Final exam marks must be between 0 and 40.")]
        public double FinalExam { get; set; } = 0;

        public double Total { get; set; } = 0;

        [MaxLength(5)]
        public string LetterGrade { get; set; } = "F";

        public double GradePoint { get; set; } = 0.00;

        [MaxLength(50)]
        public string Remarks { get; set; } = "Fail";

        public bool IsPublished { get; set; } = false;

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
