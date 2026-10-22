using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Models
{
    [Table("cart_items")]
    public class CartItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
       
        [Column("user_id")]
        public Guid UserId { get; set; }      // = ProfileModel.Id
        
        [Column("product_id")]
        public Guid ProductId { get; set; }   // = Product.Id
        public int Quantity { get; set; } = 1;
       
        [Column("added_at")]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;   // ต้อง UtcNow เพราะ Postgres (Npgsql) ไม่รับ DateTime.Now

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}