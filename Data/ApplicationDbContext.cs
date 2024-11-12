using Microsoft.EntityFrameworkCore;
using OrderManagementApp.Models;

namespace OrderManagementApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = default!;

        public DbSet<OrderDetail> OrderDetails { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().ToTable("products");
            
            modelBuilder.Entity<Category>().ToTable("categories");
            
            modelBuilder.Entity<Customer>().ToTable("customers");
            
            modelBuilder.Entity<Order>().ToTable("orders");

            modelBuilder.Entity<OrderDetail>().ToTable("order_details");

            base.OnModelCreating(modelBuilder);
        }
    }
}
