using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace finalSE.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        [Display(Name = "Subject Name")]
        public string SubjectName { get; set; }

        [MaxLength(20)]
        [Display(Name = "Subject Code")]
        public string SubjectCode { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual DepartmentModel Department { get; set; }
    }
}
