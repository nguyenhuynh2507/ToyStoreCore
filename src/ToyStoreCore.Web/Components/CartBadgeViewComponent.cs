using Microsoft.AspNetCore.Mvc;
using ToyStoreCore.Web.Services;

namespace ToyStoreCore.Web.Components;

public class CartBadgeViewComponent : ViewComponent
{
    private readonly CartService _cartService;

    public CartBadgeViewComponent(CartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cart = await _cartService.GetCartAsync();
        var count = cart.Lines.Sum(l => l.Quantity);
        return View(count);
    }
}
