using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        context.Database.EnsureCreated();

        SeedProducts(context);
        SeedMembershipTiers(context);
        SeedPromotions(context);
        SeedBanners(context);
    }

    private static void SeedProducts(AppDbContext context)
    {
        if (context.Categories.Any())
        {
            return; // already seeded
        }

        var categories = new List<Category>
        {
            new() { Name = "Xe điều khiển" },
            new() { Name = "Búp bê" },
            new() { Name = "Lego & Xếp hình" },
            new() { Name = "Đồ chơi giáo dục" },
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();

        var products = new List<Product>
        {
            new()
            {
                Name = "Xe điều khiển địa hình 4WD",
                Description = "Xe RC leo địa hình, pin sạc, chống nước nhẹ.",
                Price = 590000,
                StockQuantity = 25,
                ImageUrl = "https://placehold.co/400x300?text=RC+Car",
                CategoryId = categories[0].Id,
            },
            new()
            {
                Name = "Búp bê công chúa tóc dài",
                Description = "Búp bê nhựa an toàn, có thể thay trang phục.",
                Price = 250000,
                StockQuantity = 40,
                ImageUrl = "https://placehold.co/400x300?text=Doll",
                CategoryId = categories[1].Id,
            },
            new()
            {
                Name = "Bộ Lego thành phố 500 mảnh",
                Description = "Bộ xếp hình 500 mảnh, chủ đề thành phố.",
                Price = 780000,
                StockQuantity = 15,
                ImageUrl = "https://placehold.co/400x300?text=Lego",
                CategoryId = categories[2].Id,
            },
            new()
            {
                Name = "Bảng chữ cái thông minh",
                Description = "Đồ chơi giáo dục giúp bé học chữ cái và số.",
                Price = 190000,
                StockQuantity = 60,
                ImageUrl = "https://placehold.co/400x300?text=Learning+Toy",
                CategoryId = categories[3].Id,
            },
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }

    private static void SeedMembershipTiers(AppDbContext context)
    {
        if (context.MembershipTiers.Any())
        {
            return;
        }

        context.MembershipTiers.AddRange(
            new MembershipTier { TierName = "Đồng", RequiredOrders = 0, DiscountPercent = 0 },
            new MembershipTier { TierName = "Bạc", RequiredOrders = 3, DiscountPercent = 3 },
            new MembershipTier { TierName = "Vàng", RequiredOrders = 6, DiscountPercent = 5 },
            new MembershipTier { TierName = "Kim Cương", RequiredOrders = 10, DiscountPercent = 10 }
        );
        context.SaveChanges();
    }

    private static void SeedPromotions(AppDbContext context)
    {
        if (context.Promotions.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        context.Promotions.AddRange(
            new Promotion
            {
                PromotionCode = "SALE10",
                PromotionName = "Giảm 10% cho đơn từ 200.000đ",
                DiscountType = PromotionDiscountType.Percentage,
                DiscountValue = 10,
                MinOrderValue = 200000,
                StartDate = now.AddDays(-1),
                EndDate = now.AddMonths(3),
                UsageLimit = 0,
                UsedCount = 0,
                IsActive = true,
            },
            new Promotion
            {
                PromotionCode = "FREESHIP30",
                PromotionName = "Giảm 30.000đ cho đơn từ 300.000đ",
                DiscountType = PromotionDiscountType.FixedAmount,
                DiscountValue = 30000,
                MinOrderValue = 300000,
                StartDate = now.AddDays(-1),
                EndDate = now.AddMonths(3),
                UsageLimit = 100,
                UsedCount = 0,
                IsActive = true,
            }
        );
        context.SaveChanges();
    }

    private static void SeedBanners(AppDbContext context)
    {
        if (context.Banners.Any())
        {
            return;
        }

        context.Banners.AddRange(
            new Banner
            {
                Title = "Khuyến mãi mùa hè",
                ImageUrl = "https://placehold.co/1200x400?text=Khuyen+Mai+He",
                LinkUrl = "/Products",
                IsActive = true,
            },
            new Banner
            {
                Title = "Hàng mới về",
                ImageUrl = "https://placehold.co/1200x400?text=Hang+Moi+Ve",
                LinkUrl = "/Products",
                IsActive = true,
            }
        );
        context.SaveChanges();
    }
}
