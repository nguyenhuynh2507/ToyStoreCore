using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers;

public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MembershipService _membershipService;
    private readonly PromotionService _promotionService;

    public OrdersController(
        AppDbContext db,
        CartService cartService,
        UserManager<ApplicationUser> userManager,
        MembershipService membershipService,
        PromotionService promotionService)
    {
        _db = db;
        _cartService = cartService;
        _userManager = userManager;
        _membershipService = membershipService;
        _promotionService = promotionService;
    }

    // POST /Orders/BuyNow - "Mua ngay" from the product details page, skips the cart entirely.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product is null || !product.IsActive)
        {
            TempData["Error"] = "Sản phẩm không còn khả dụng.";
            return RedirectToAction("Index", "Products");
        }

        if (quantity < 1)
        {
            quantity = 1;
        }

        if (product.StockQuantity < quantity)
        {
            TempData["Error"] = $"Sản phẩm chỉ còn {product.StockQuantity} sản phẩm trong kho.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        _cartService.SetQuickBuy(new CartLine
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            ImageUrl = product.ImageUrl,
            Quantity = quantity,
        });

        return RedirectToAction(nameof(Checkout), new { quickBuy = true });
    }

    // GET /Orders/Checkout
    public async Task<IActionResult> Checkout(bool quickBuy = false, List<int>? ids = null)
    {
        var (cart, resolvedQuickBuy) = await ResolveCheckoutCartAsync(quickBuy, ids);
        if (!cart.Lines.Any())
        {
            return resolvedQuickBuy
                ? RedirectToAction("Index", "Products")
                : RedirectToAction("Index", "Cart");
        }

        var model = new CheckoutViewModel { Cart = cart, IsQuickBuy = resolvedQuickBuy, SelectedProductIds = ids ?? new List<int>() };
        await ApplyMembershipDiscountAsync(model);
        return View(model);
    }

    /// <summary>
    /// Builds the cart to check out against: the quick-buy line, a filtered subset of the
    /// persisted cart (customer selected specific items), or the whole cart.
    /// </summary>
    private async Task<(CartViewModel Cart, bool IsQuickBuy)> ResolveCheckoutCartAsync(bool quickBuy, List<int>? ids)
    {
        if (quickBuy)
        {
            var line = _cartService.GetQuickBuy();
            var quickCart = new CartViewModel();
            if (line is not null)
            {
                quickCart.Lines.Add(line);
            }

            return (quickCart, true);
        }

        var fullCart = await _cartService.GetCartAsync();
        if (ids is { Count: > 0 })
        {
            var subset = new CartViewModel
            {
                Lines = fullCart.Lines.Where(l => ids.Contains(l.ProductId)).ToList(),
            };

            // Re-validate the voucher against the subset total - it may no longer qualify
            // (e.g. MinOrderValue) once only part of the cart is being paid for.
            if (!string.IsNullOrEmpty(fullCart.AppliedPromoCode))
            {
                var check = await _promotionService.ApplyPromotionAsync(fullCart.AppliedPromoCode, subset.Total);
                if (check.Success)
                {
                    subset.AppliedPromoCode = fullCart.AppliedPromoCode;
                    subset.AppliedPromoDiscount = check.DiscountValue;
                }
            }

            return (subset, false);
        }

        return (fullCart, false);
    }

    private async Task ApplyMembershipDiscountAsync(CheckoutViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = _userManager.GetUserId(User);
        var (percent, tierLabel) = await _membershipService.GetDiscountPercentAsync(userId);
        model.MembershipDiscountPercent = percent;
        model.MembershipTierLabel = tierLabel;

        if (percent > 0)
        {
            var value = model.Cart.FinalTotal * (percent / 100m);
            model.MembershipDiscountValue = value < 0 ? 0 : value;
        }
    }

    // POST /Orders/Checkout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model, bool isQuickBuy, List<int>? selectedProductIds)
    {
        var (cart, resolvedQuickBuy) = await ResolveCheckoutCartAsync(isQuickBuy, selectedProductIds);
        model.Cart = cart;
        model.IsQuickBuy = resolvedQuickBuy;
        model.SelectedProductIds = selectedProductIds ?? new List<int>();
        await ApplyMembershipDiscountAsync(model);

        if (!cart.Lines.Any())
        {
            ModelState.AddModelError(string.Empty, "Không có sản phẩm nào để đặt hàng.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Re-check stock against DB right before creating the order.
        var productIds = cart.Lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var line in cart.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
            {
                ModelState.AddModelError(string.Empty, $"Sản phẩm \"{line.ProductName}\" không còn khả dụng.");
                continue;
            }

            if (product.StockQuantity < line.Quantity)
            {
                ModelState.AddModelError(string.Empty,
                    $"Sản phẩm \"{product.Name}\" chỉ còn {product.StockQuantity} sản phẩm trong kho.");
            }
        }

        // Re-validate the voucher server-side right before charging - it may have expired
        // or hit its usage limit since it was applied at the cart step.
        if (!string.IsNullOrEmpty(cart.AppliedPromoCode))
        {
            var promoCheck = await _promotionService.ApplyPromotionAsync(cart.AppliedPromoCode, cart.Total);
            if (!promoCheck.Success)
            {
                ModelState.AddModelError(string.Empty, $"Mã khuyến mãi không còn hợp lệ: {promoCheck.Message}");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var order = new Order
        {
            UserId = User.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(User)
                : null,
            CustomerName = model.CustomerName,
            PhoneNumber = model.PhoneNumber,
            ShippingAddress = model.ShippingAddress,
            Note = model.Note,
            Status = OrderStatus.Pending,
            Subtotal = cart.Total,
            MembershipDiscount = model.MembershipDiscountValue,
            PromoCode = cart.AppliedPromoCode,
            PromoDiscount = cart.AppliedPromoDiscount,
            TotalAmount = model.FinalTotal,
        };

        foreach (var line in cart.Lines)
        {
            var product = products[line.ProductId];
            product.StockQuantity -= line.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = line.Quantity,
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(order.PromoCode))
        {
            await _promotionService.IncrementUsedCountAsync(order.PromoCode);
        }

        if (resolvedQuickBuy)
        {
            _cartService.ClearQuickBuy();
        }
        else if (model.SelectedProductIds.Count > 0)
        {
            await _cartService.RemoveItemsAsync(model.SelectedProductIds);
        }
        else
        {
            await _cartService.ClearAsync();
        }

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    // GET /Orders/Confirmation/5
    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }

    // GET /Orders/MyOrders
    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var userId = _userManager.GetUserId(User);
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    // POST /Orders/Cancel/5 - customer cancels their own order, only while still Pending.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

        if (order is null || order.UserId != userId)
        {
            return NotFound();
        }

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể hủy đơn khi đơn đang ở trạng thái Chờ xử lý.";
            return RedirectToAction(nameof(Confirmation), new { id });
        }

        var productIds = order.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        foreach (var item in order.Items)
        {
            if (products.TryGetValue(item.ProductId, out var product))
            {
                product.StockQuantity += item.Quantity;
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Đã hủy đơn hàng.";
        return RedirectToAction(nameof(Confirmation), new { id });
    }
}
