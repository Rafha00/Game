using Microsoft.EntityFrameworkCore;
using Game.Models;

namespace Game.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

                 public DbSet<ProfileModel> Profiles { get; set; }
                   public DbSet<Product> Products { get; set; }
                   public DbSet<CartItem> CartItems { get; set; }
                   public DbSet<DiscountCode> DiscountCodes { get; set; }
                   
                    public DbSet<Order> Orders {get; set;}
                    public DbSet<OrderItem> OrderItems {get; set;}


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=db.dtyjkwuezgxzoojgcwaa.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=@Thanin7864");
            }
        }
    }
}