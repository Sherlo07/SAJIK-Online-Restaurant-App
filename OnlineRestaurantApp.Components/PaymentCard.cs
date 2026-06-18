using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class PaymentCard
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentCardId { get; set; }

        public int UserId { get; set; }

        [Required]
        
        public long CardNumber { get; set; } // store masked or tokenized ideally

        [StringLength(50)]
        public string? CardHolderName { get; set; }

        // If you're on EF Core < 7, map DateOnly to DateTime via ValueConverter (shown below)
        public DateOnly ExpiryDate { get; set; }

        // do not store CVV
        [Required]
        public int CVV { get; set; }

        public bool Status { get; set; }

        public User? User { get; set; }
    }
}
