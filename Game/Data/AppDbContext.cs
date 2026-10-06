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
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // เปลี่ยน Connection String เป็นรูปแบบของ MySQL
                var connectionString = "Server=localhost;Port=3306;Database=your_mysql_db_name;User=root;Password=your_password;";
                
                // เปลี่ยนมาใช้ UseMySql ของ Pomelo
              optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)));
            }
        }
    }
}