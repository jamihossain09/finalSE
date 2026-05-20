using System.ComponentModel.DataAnnotations;

namespace finalSE.Models
{
    public class DepartmentModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; }
    }
}       