using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class User
    {
        [Key]  // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string? UserFirstName { get; set; }

        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string? UserLastName { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool Status { get; set; } = true; 

        [MaxLength(100, ErrorMessage = "Invalid email format")] 
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [MaxLength(10, ErrorMessage = "Invalid size")]
        public string Mobile { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(50)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(50)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
        

        //foreign key
        //[Required]
        [ForeignKey("Role")]
        public int RoleId { get; set; }

        //navigation property - one user is mapped to only one role
        public Role? role { get; set; }

    }
}
