using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole + "," + IdentitySeeder.StaffRole)]
[Route("Admin/Pos")]
public class AdminPosController : Controller
{
    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tiền mặt",
        "Chuyển khoản (QR)",
    };

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MembershipService _membershipService;
    private readonly ILogger<AdminPosController> _logger;

    public AdminPosController(AppDbContext db, UserManager<ApplicationUser> userManager, MembershipService membershipService, ILogger<AdminPosController> logger)
    {
        _db = db;
        _userManager = userManager;
        _membershipService = membershipService;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Bán hàng tại quầy (POS)";
        return View();
    }

    [HttpGet("SearchProducts")]
    public async Task<IActionResult> SearchProducts(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(Array.Empty<object>());
        }

        var keyword = query.Trim();
        var suggestions = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Name.Contains(keyword))
            .OrderBy(p => p.Name)
            .Take(8)
            .Select(p => new { p.Id, p.Name, ImageUrl = p.ImageUrl ?? string.Empty, p.Price, p.StockQuantity })
            .ToListAsync();

        return Json(suggestions);
    }

    [HttpGet("LookupCustomer")]
    public async Task<IActionResult> LookupCustomer(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Json(new { found = false, message = "Vui lòng nhập số điện thoại." });
        }

        var normalized = phone.Trim();
        var customer = await _db.Users.Include(u => u.Tier)
            .Where(u => u.Phone != null && u.Phone.Trim() == normalized)
            .FirstOrDefaultAsync();

        if (customer is null)
        {
            return Json(new { found = false, message = "Không tìm thấy khách hàng với SĐT này." });
        }

        if (customer.IsLocked)
        {
            return Json(new { found = false, message = "Tài khoản khách hàng đang bị khóa." });
        }

        return Json(new
        {
            found = true,
            customerId = customer.Id,
            fullName = customer.FullName,
            phone = customer.Phone,
            tierName = customer.Tier?.TierName ?? "Chưa có hạng",
            discountPercent = customer.Tier?.DiscountPercent ?? 0m,
            totalCompletedOrders = customer.TotalCompletedOrders,
        });
    }

    [HttpPost("CreatePosOrder")]
    public async Task<IActionResult> CreatePosOrder([FromBody] PosOrderModel model)
    {
        if (model?.Items is null || model.Items.Count == 0)
        {
            return Json(new { success = false, message = "Giỏ hàng trống, không thể thanh toán." });
        }

        if (string.IsNullOrWhiteSpace(model.PaymentMethod) || !AllowedPaymentMethods.Contains(model.PaymentMethod.Trim()))
        {
            return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ." });
        }

        var paymentMethod = model.PaymentMethod.Trim();
        var lineItems = model.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new PosOrderItemModel { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (lineItems.Count == 0)
        {
            return Json(new { success = false, message = "Danh sách sản phẩm không hợp lệ." });
        }

        var productIds = lineItems.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        decimal subtotal = 0;
        foreach (var item in lineItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return Json(new { success = false, message = $"Sản phẩm #{item.ProductId} không tồn tại." });
            }

            if (!product.IsActive)
            {
                return Json(new { success = false, message = $"Sản phẩm \"{product.Name}\" hiện không được bán." });
            }

            if (product.StockQuantity < item.Quantity)
            {
                return Json(new { success = false, message = $"\"{product.Name}\" không đủ tồn kho (còn {product.StockQuantity})." });
            }

            item.UnitPrice = product.Price;
            subtotal += item.UnitPrice * item.Quantity;
        }

        ApplicationUser? customer = null;
        if (!string.IsNullOrEmpty(model.CustomerId))
        {
            customer = await _db.Users.Include(u => u.Tier).FirstOrDefaultAsync(u => u.Id == model.CustomerId);
        }

        decimal membershipDiscount = 0;
        if (customer?.Tier is { DiscountPercent: > 0 })
        {
            membershipDiscount = subtotal * (customer.Tier.DiscountPercent / 100m);
        }

        var totalAmount = subtotal - membershipDiscount;
        if (totalAmount < 0)
        {
            totalAmount = 0;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserId = customer?.Id,
                CustomerName = customer?.FullName ?? "Khách lẻ",
                PhoneNumber = customer?.Phone ?? "—",
                ShippingAddress = "Mua tại quầy",
                Status = OrderStatus.Completed,
                OrderType = "POS",
                PaymentMethod = paymentMethod,
                Subtotal = subtotal,
                MembershipDiscount = membershipDiscount,
                TotalAmount = totalAmount,
            };

            foreach (var item in lineItems)
            {
                var product = products[item.ProductId];
                product.StockQuantity -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                });
            }

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            if (customer is not null)
            {
                await _membershipService.RecalculateTierAsync(customer.Id);
            }

            await transaction.CommitAsync();

            return Json(new
            {
                success = true,
                message = "Thanh toán POS thành công!",
                orderId = order.Id,
                subtotal,
                membershipDiscount,
                totalAmount,
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi tạo đơn POS");
            return Json(new { success = false, message = "Không thể tạo đơn POS. Vui lòng thử lại." });
        }
    }
}
