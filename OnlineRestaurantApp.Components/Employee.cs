using System;

using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

namespace OnlineRestaurantApp.Components
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        [Required, StringLength(100)]

        public string FullName { get; set; } = "";

        [StringLength(120)]

        public string? Email { get; set; }

        [StringLength(20)]

        public string? Phone { get; set; }

        [Required, StringLength(30)]

        public string Role { get; set; } = "Delivery";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation

        public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    }

}
