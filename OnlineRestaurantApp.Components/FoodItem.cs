using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class FoodItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ItemId {  get; set; }

        [Required]
        [ForeignKey("Category")]
        public int CategoryId {  get; set; }

        [Required]
        [ForeignKey("ItemType")]
        public int ItemTypeId {  get; set; }

        [Required]
        [MaxLength(50, ErrorMessage = "Invalid size")]
        [MinLength(3, ErrorMessage = "Invalid size")]
        public string? ItemName {  get; set; }

        [Required]
        [Range(50,1000,ErrorMessage ="Invalid Value")]
        public int ActualPrice {  get; set; }

        [Required(ErrorMessage ="Cannot be null")]
        public decimal Rating {  get; set; }

        [Required(ErrorMessage = "Invalid Value")]
        
        public int RatingBy {  get; set; }
        public string? ItemImagePath {  get; set; }

        [Required]
        [MaxLength(300, ErrorMessage = "Invalid max size")]
        [MinLength(3, ErrorMessage = "Invalid size")]
        public string? ItemDescription {  get; set; }

        public bool IsAvailable {  get; set; }
        public bool IsBestSeller {  get; set; }
        public bool IsBreakFast { get; set; }
        public bool IsLunch { get; set; }
        public bool IsSnack { get; set; }
        public bool IsDinner { get; set; }


        [Required]
        [Range(0, 100, ErrorMessage = "Invalid Value")]
        public int DiscountPer {  get; set; }

        [Required]
        [Range(50, 1000, ErrorMessage = "Invalid Value")]
        public int SellingPrice {  get; set; }
        public bool IsFastMoving {  get; set; }

        [NotMapped]
        public IFormFile? ItemImage {  get; set; }

        //navigation properties
        public Category? Category { get; set; }

        public ItemType? ItemType { get; set; }

    }
}
