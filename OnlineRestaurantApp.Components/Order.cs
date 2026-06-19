using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal OrderAmount { get; set; }
        public bool IsAssigned { get; set; } = false;

        // navigation properties - one order is mapped to one user only
        [ForeignKey("UserId")]
        public User? user { get; set; }

        // navigation properties - one order is mapped to one or more orderitem details
        public List<OrderItemDetails>? orderDetailsItems { get; set; }

        // np - one order is mapped to one payment
        public Payment? payment { get; set; }
        public Delivery? Delivery { get; set; }

    }
}