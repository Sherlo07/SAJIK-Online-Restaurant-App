using System;

using System.ComponentModel.DataAnnotations;

namespace OnlineRestaurantApp.Components
{
    public class Delivery
    {
        public int DeliveryId { get; set; }

        public int OrderId { get; set; }

        public int EmployeeId { get; set; }

        public int AddressId { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.UtcNow;

        public DateTime? ExpectedOn { get; set; }

        [Required, StringLength(30)]

        public string DeliveryStatus { get; set; } = "Assigned";

        // Navigation

        public Order? Order { get; set; }

        public Employee? Employee { get; set; }

        public DeliveryAddress? Address { get; set; }

    }

}
