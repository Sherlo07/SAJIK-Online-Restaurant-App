using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [RegularExpression("^[0-9]{3,3}$")]
        public int CategoryId {  get; set; }

        [Required(ErrorMessage = "Name cannot be null")]
        [MaxLength(50, ErrorMessage = "Invalid size")]
        [MinLength(4, ErrorMessage = "Invalid size")]
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Name cannot be null")]
        [MaxLength(200, ErrorMessage = "Invalid size")]
        [MinLength(50, ErrorMessage = "Invalid size")]
        public string? CategoryDescritpion {  get; set; }
        public string? CategoryImagePath { get; set; }
        public bool CategoryStatus {  get; set; }
        [Required(ErrorMessage = "Name cannot be null")]
        [Range(0, 100, ErrorMessage = "Invalid size")]
        public int CategoryDiscount {  get; set; }


        [NotMapped]
        public IFormFile? CategoryImage { get; set; }

        public List<FoodItem>? FoodItems { get; set; }

    }
}
