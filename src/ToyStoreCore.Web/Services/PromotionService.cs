using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Services;

public record PromotionResult(bool Success, decimal DiscountValue, string Message);

/// <summary>
/// Validates and applies promotion codes. Ported from ToyStore's SP_APPLY_PROMOTION
/// Oracle stored procedure - reimplemented as plain C# since we're on PostgreSQL now.
/// </summary>
public class PromotionService
{
    private readonly AppDbContext _db;

    public PromotionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PromotionResult> ApplyPromotionAsync(string? promoCode, decimal orderValue)
    {
        if (string.IsNullOrWhiteSpace(promoCode))
        {
            return new PromotionResult(false, 0, "Vui lòng nhập mã khuyến mãi.");
        }

        var code = promoCode.Trim();
        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionCode == code);

        if (promotion is null)
        {
            return new PromotionResult(false, 0, "Mã khuyến mãi không tồn tại.");
        }

        if (!promotion.IsActive)
        {
            return new PromotionResult(false, 0, "Mã khuyến mãi đã bị vô hiệu hóa.");
        }

        var now = DateTime.UtcNow;
        if (now < promotion.StartDate || now > promotion.EndDate)
        {
            return new PromotionResult(false, 0, "Mã khuyến mãi đã hết hạn hoặc chưa bắt đầu.");
        }

        if (promotion.UsageLimit > 0 && promotion.UsedCount >= promotion.UsageLimit)
        {
            return new PromotionResult(false, 0, "Mã khuyến mãi đã hết lượt sử dụng.");
        }

        if (orderValue < promotion.MinOrderValue)
        {
            return new PromotionResult(false, 0,
                $"Đơn hàng cần tối thiểu {promotion.MinOrderValue:N0}đ để dùng mã này.");
        }

        var discount = promotion.DiscountType == PromotionDiscountType.Percentage
            ? orderValue * (promotion.DiscountValue / 100m)
            : promotion.DiscountValue;

        if (discount < 0) discount = 0;
        if (discount > orderValue) discount = orderValue;

        return new PromotionResult(true, discount, "Áp dụng mã thành công.");
    }

    /// <summary>Call once, right after an order using this promo code is successfully placed.</summary>
    public async Task<bool> IncrementUsedCountAsync(string promotionCode)
    {
        var code = promotionCode.Trim();
        var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionCode == code);
        if (promo is null)
        {
            return false;
        }

        if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
        {
            return false;
        }

        promo.UsedCount += 1;
        await _db.SaveChangesAsync();
        return true;
    }
}
