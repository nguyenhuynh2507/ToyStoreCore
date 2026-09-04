using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptDetail> GoodsReceiptDetails => Set<GoodsReceiptDetail>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // required - configures Identity's own tables

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order!)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.Tier)
            .WithMany()
            .HasForeignKey(u => u.TierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Promotion>()
            .HasIndex(p => p.PromotionCode)
            .IsUnique();

        modelBuilder.Entity<Cart>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceipt>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceiptDetail>()
            .HasOne(d => d.GoodsReceipt)
            .WithMany(r => r.Details)
            .HasForeignKey(d => d.GoodsReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoodsReceiptDetail>()
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserActivityLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserActivityLog>()
            .HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductImage>()
            .HasOne(pi => pi.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkShift>()
            .HasOne(w => w.Staff)
            .WithMany()
            .HasForeignKey(w => w.StaffUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payroll>()
            .HasOne(p => p.Staff)
            .WithMany()
            .HasForeignKey(p => p.StaffUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
