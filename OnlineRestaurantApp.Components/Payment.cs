
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class Payment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        [ForeignKey("OrderId")]
        public int OrderId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }

        // Use FK to PaymentType
        [ForeignKey("PaymentTypeId")]
        public int PaymentTypeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderAmount { get; set; }

        public DateTime? PaidOnDate { get; set; }

        // clearer than "Status"
        public bool Status { get; set; }

        // Navigation props (no need for [ForeignKey] if convention names are used)
        public Order? Order { get; set; }
        public User? User { get; set; }
        public PaymentType? PaymentType { get; set; }
    }
}