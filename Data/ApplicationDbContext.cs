using Microsoft.EntityFrameworkCore;
using OrderManagementApp.Models;
using System.Reflection.Metadata;

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

            modelBuilder.Entity<Customer>()
                .HasMany(e => e.Orders)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .IsRequired(false);

            modelBuilder.Entity<Order>()
                .HasMany(e => e.OrderDetails)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .IsRequired(false);

            modelBuilder.Entity<Product>()
                .HasMany(e => e.OrderDetails)
                .WithOne(e => e.Product)
                .HasForeignKey(e => e.ProductId)
                .IsRequired(false);
            
            modelBuilder.Entity<Category>()
                .HasMany(e => e.Products)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientCascade);

            base.OnModelCreating(modelBuilder);
        }      
    }
}
