using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("Profiles")]
    public class ProfileModel
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        

        [Column("role")]
        public string Role { get; set; } = "customer";
    }
}