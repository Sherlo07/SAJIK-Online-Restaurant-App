using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineRestaurantApp.Components
{
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Empty role name is not allowed")]
        [MaxLength(15, ErrorMessage = "Invalid role name size")]
        public string RoleName { get; set; } = string.Empty;

        //Navigation property - under one role , there can be zero or one or users
        public List<User> users { get; set; }

    }
}
