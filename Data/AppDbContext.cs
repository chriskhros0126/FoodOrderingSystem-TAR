using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using FoodOrderingSystem.Models;

namespace FoodOrderingSystem.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Existing DbSets
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Rider> Riders { get; set; }
        public DbSet<TableReservation> TableReservations { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DishIngredient> DishIngredients { get; set; }
        
        // Add the new DbSet for Inventory
        public DbSet<InventoryItem> InventoryItems { get; set; }
        
        // Added DbSets for Coupon, Invoice, and Payment
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        
        // Add DbSet for Feedback
        public DbSet<Feedback> Feedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Existing configurations
            modelBuilder.Entity<Dish>()
                .Property(d => d.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            // Existing relationships
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Dish)
                .WithMany()
                .HasForeignKey(oi => oi.DishId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApplicationUser>()
                .HasOne(au => au.IdentityUser)
                .WithOne()
                .HasForeignKey<ApplicationUser>(au => au.Id);

            // Delivery relationships
            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Delivery>()
                .HasOne(d => d.Rider)
                .WithMany()
                .HasForeignKey(d => d.RiderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add configuration for InventoryItem if needed
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.CostPerUnit)
                .HasPrecision(18, 2);
                
            // Configure relationships for DishIngredient
            modelBuilder.Entity<DishIngredient>()
                .HasOne(di => di.Dish)
                .WithMany(d => d.DishIngredients)
                .HasForeignKey(di => di.DishId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DishIngredient>()
                .HasOne(di => di.InventoryItem)
                .WithMany(i => i.DishIngredients)
                .HasForeignKey(di => di.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure relationships for new entities
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Invoices)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure precision for Payment.Amount
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
                
            // Configure precision for Coupon.DiscountValue
            modelBuilder.Entity<Coupon>()
                .Property(c => c.DiscountValue)
                .HasPrecision(18, 2);
                
            // Configure Order-Coupon relationship
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // The Feedback-Order relationship has been removed as it's no longer needed
        }
    }
}