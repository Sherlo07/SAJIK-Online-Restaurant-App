using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class OrderItemDetails
    {
        [Key]
        public int OrderItemId { get; set; }

        [ForeignKey("OrderId")]
        public int OrderId { get; set; }

        [ForeignKey("ItemId")]
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal ItemAmount { get; set; }

        // Navigation Properties
        public Order? order { get; set; }



    }
}