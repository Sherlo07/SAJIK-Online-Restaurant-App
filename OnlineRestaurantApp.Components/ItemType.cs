using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class ItemType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ItemTypeId {  get; set; }

        [Required]
        [MaxLength(30,ErrorMessage ="Invalid size")]
        [MinLength(3,ErrorMessage ="Invalid size")]
        public string? ItemTypeName {  get; set; }

        //navigation property
        public List<FoodItem> FoodItems { get; set; }
    }
}
