using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/Promotions")]
public class AdminPromotionsController : Controller
{
    private readonly AppDbContext _db;

    public AdminPromotionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? searchString)
    {
        ViewData["CurrentFilter"] = searchString;

        var query = _db.Promotions.AsQueryable();
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(p =>
                p.PromotionCode.Contains(searchString) || p.PromotionName.Contains(searchString));
        }

        var promotions = await query.OrderByDescending(p => p.Id).ToListAsync();
        return View(promotions);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View(new Promotion
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(7),
            IsActive = true,
        });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Promotion promotion)
    {
        await ValidateBusinessRulesAsync(promotion);

        if (!ModelState.IsValid)
        {
            return View(promotion);
        }

        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Thêm mã khuyến mãi mới thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var promotion = await _db.Promotions.FindAsync(id);
        if (promotion is null)
        {
            return NotFound();
        }

        return View(promotion);
    }

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Promotion promotion)
    {
        if (id != promotion.Id)
        {
            return BadRequest();
        }

        await ValidateBusinessRulesAsync(promotion);

        if (!ModelState.IsValid)
        {
            return View(promotion);
        }

        var existing = await _db.Promotions.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        // Gia hạn mã đã hết hạn: kích hoạt ngay thay vì chờ StartDate ở tương lai.
        // Ported từ PromotionDateHelper.NormalizeOnReactivation của bản cũ.
        var now = DateTime.Now;
        if (promotion.IsActive && promotion.EndDate >= now && existing.EndDate < now && promotion.StartDate > now)
        {
            promotion.StartDate = now;
        }

        existing.PromotionCode = promotion.PromotionCode;
        existing.PromotionName = promotion.PromotionName;
        existing.DiscountType = promotion.DiscountType;
        existing.DiscountValue = promotion.DiscountValue;
        existing.MinOrderValue = promotion.MinOrderValue;
        existing.StartDate = promotion.StartDate;
        existing.EndDate = promotion.EndDate;
        existing.UsageLimit = promotion.UsageLimit;
        existing.IsActive = promotion.IsActive;

        await _db.SaveChangesAsync();

        TempData["Message"] = "Cập nhật mã khuyến mãi thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var promotion = await _db.Promotions.FindAsync(id);
        if (promotion is null)
        {
            TempData["Error"] = "Không tìm thấy mã khuyến mãi cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Xóa mã khuyến mãi thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateBusinessRulesAsync(Promotion promotion)
    {
        var codeExists = await _db.Promotions.AnyAsync(p =>
            p.PromotionCode == promotion.PromotionCode && p.Id != promotion.Id);
        if (codeExists)
        {
            ModelState.AddModelError(nameof(Promotion.PromotionCode), "Mã code này đã tồn tại, vui lòng chọn mã khác.");
        }

        if (promotion.EndDate < promotion.StartDate)
        {
            ModelState.AddModelError(nameof(Promotion.EndDate), "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        if (promotion.DiscountType == PromotionDiscountType.Percentage && promotion.DiscountValue > 100)
        {
            ModelState.AddModelError(nameof(Promotion.DiscountValue), "Giảm theo % không được vượt quá 100.");
        }
    }
}
