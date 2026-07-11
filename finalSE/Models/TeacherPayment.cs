using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace finalSE.Models
{
    public class TeacherPayment
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int TeacherID { get; set; }

        [ForeignKey("TeacherID")]
        public Teacher Teacher { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Month { get; set; } // e.g. "January 2026", "July 2026"

        [StringLength(100)]
        public string? TransactionID { get; set; } // bKash TrxID

        [StringLength(100)]
        public string? BkashPaymentID { get; set; } // bKash internal PaymentID

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending / Paid / Failed
    }
}
