
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class Slider
    {
        // Primary Key (no identity per your code)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [RegularExpression("^[0-9]{3,3}$")]
        public int SliderId { get; set; }

        // nvarchar(50) NOT NULL
        [Required(ErrorMessage = "Slider text cannot be null")]
        [MaxLength(50, ErrorMessage = "Slider text exceeds 50 characters")]
        [MinLength(2, ErrorMessage = "Slider text must be at least 2 characters")]
        public string SliderText { get; set; } = string.Empty;

        // nvarchar(100) NOT NULL
        [Required(ErrorMessage = "Link cannot be null")]
        [MaxLength(100, ErrorMessage = "Link exceeds 100 characters")]
        [MinLength(5, ErrorMessage = "Link must be at least 5 characters")]
        
        
        public string SliderTextLink { get; set; } = string.Empty;

        // int NOT NULL
        [Required(ErrorMessage = "Order number is required")]
        [Range(1, 1000, ErrorMessage = "Order number must be between 1 and 1000")]
        
        public int SliderOrderNo { get; set; }

        // datetime NOT NULL
        [Required(ErrorMessage = "CreatedOn is required")]
       
        public DateTime CreatedOn { get; set; }

        // bit NOT NULL
        
        public bool Status { get; set; }

        // nvarchar(50) NOT NULL
        
        
        public string SliderImagePath { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? SliderImage {get; set; }
    }
}
