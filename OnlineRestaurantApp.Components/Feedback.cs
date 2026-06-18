using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public enum FbStatus
    {
        Open,
        UnderProcessing,
        Closed

    }
    public class Feedback
    {

        [Key] // Identity column
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FeedbackId { get; set; }

        [Required(ErrorMessage = "Empty value is not allowed")]
        [MaxLength(50, ErrorMessage = "Invalid size")]
        [MinLength(3, ErrorMessage = "Invalid size")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Empty value is not allowed")]
        [MaxLength(50, ErrorMessage = "Invalid size")]
        [MinLength(3, ErrorMessage = "Invalid size")]
        public string Subject { get; set; } = string.Empty;

        public string? FeedbackImagePath { get; set; }

        [Required(ErrorMessage = "Empty value is not allowed")]
        [MaxLength(500, ErrorMessage = "Invalid size")]
        [MinLength(30, ErrorMessage = "Invalid size")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Empty value is not allowed")]
        [EmailAddress(ErrorMessage = "Invalid value")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A phone number is required.")]
        [RegularExpression(@"^([0-9]{10})$", ErrorMessage = "Invalid Mobile Number.")]
        [Display(Name = "Mobile Number")]
        public string Mobile { get; set; } = string.Empty;
        public FbStatus Status { get; set; }
        public string RemarksByAdmin { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }

        [NotMapped]
        public IFormFile? ItemImage { get; set; }
    }
}