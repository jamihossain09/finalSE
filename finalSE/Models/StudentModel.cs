using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class StudentModel
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int Age { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }
        // Foreign Key
        public int DepartmentId { get; set; }

        public virtual DepartmentModel? Department { get; set; }
    }
}

