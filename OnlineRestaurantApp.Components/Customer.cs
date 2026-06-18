using System.ComponentModel.DataAnnotations;

namespace OnlineRestaurantApp.Components
{
    public class Customer
    {
        [Key]  // Primary Key
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? FirstName { get; set; }

        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? LastName { get; set; }  // Nullable

        [Required(ErrorMessage = "Email is required")]
        [MaxLength(100, ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [MaxLength(20, ErrorMessage = "Invalid phone number format")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(256)]
        public string? PasswordHash { get; set; }

        [Required]
        public bool Status { get; set; } = true;  // Default active
    }
}
