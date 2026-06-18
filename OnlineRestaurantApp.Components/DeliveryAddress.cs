using System;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

namespace OnlineRestaurantApp.Components
{
    public class DeliveryAddress
    {

        [Key] // ✅ explicitly define primary key

        public int AddressId { get; set; }

        public int UserId { get; set; }

        [Required, StringLength(200)]

        public string AddressLine1 { get; set; } = "";

        [StringLength(200)]

        public string? AddressLine2 { get; set; }

        [Required, StringLength(80)]

        public string City { get; set; } = "";

        [Required, StringLength(80)]

        public string State { get; set; } = "";

        [Required, StringLength(10)]

        public string Pincode { get; set; } = "";

        [StringLength(120)]

        public string? Landmark { get; set; }

        public bool IsDefault { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation (optional but recommended)

        public User? User { get; set; }

        public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    }

}
