using Microsoft.AspNetCore.Mvc;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Controllers;

public class CartController : Controller
{
    private readonly CartService _cartService;
    private readonly PromotionService _promotionService;

    public CartController(CartService cartService, PromotionService promotionService)
    {
        _cartService = cartService;
        _promotionService = promotionService;
    }

    // GET /Cart
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync();
        return View(cart);
    }

    // POST /Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        await _cartService.AddToCartAsync(productId, quantity);
        TempData["Message"] = "Đã thêm sản phẩm vào giỏ hàng.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/UpdateQuantity
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cartService.RemoveFromCartAsync(productId);
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/ClearCart
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearAsync();
        TempData["Message"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/ApplyPromotion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPromotion(string promoCode)
    {
        var cart = await _cartService.GetCartAsync();
        var result = await _promotionService.ApplyPromotionAsync(promoCode, cart.Total);

        if (result.Success)
        {
            _cartService.ApplyPromoCode(promoCode.Trim(), result.DiscountValue);
            TempData["Message"] = result.Message;
        }
        else
        {
            _cartService.RemovePromoCode();
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // POST /Cart/RemovePromotion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemovePromotion()
    {
        _cartService.RemovePromoCode();
        return RedirectToAction(nameof(Index));
    }
}
