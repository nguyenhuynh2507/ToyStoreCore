using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Services;

/// <summary>
/// Session-backed cart for everyone (guests and logged-in customers alike), with an
/// extra DB-persisted copy for logged-in users so their cart survives across browsers/
/// devices. Ported from ToyStore's CartStorageService + CustomerCartPersistenceService.
/// Also tracks an applied voucher code (session-only - revalidated at checkout anyway),
/// ported from ToyStore's PromoSessionHelper.
/// </summary>
public class CartService
{
    private const string SessionKey = "Cart";
    private const string PromoCodeKey = "Cart.PromoCode";
    private const string PromoDiscountKey = "Cart.PromoDiscount";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;
    private readonly ActivityTrackingService _activityTracking;

    public CartService(IHttpContextAccessor httpContextAccessor, AppDbContext db, ActivityTrackingService activityTracking)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
        _activityTracking = activityTracking;
    }

    private ISession Session => _httpContextAccessor.HttpContext!.Session;

    private string? CurrentUserId =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true
            ? _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    /// <summary>Session-only read, no DB fallback. Use GetCartAsync in most places instead.</summary>
    public CartViewModel GetCart()
    {
        var json = Session.GetString(SessionKey);
        var lines = string.IsNullOrEmpty(json)
            ? new List<CartLine>()
            : JsonSerializer.Deserialize<List<CartLine>>(json) ?? new List<CartLine>();

        var cart = new CartViewModel { Lines = lines };

        var promoCode = Session.GetString(PromoCodeKey);
        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var discountStr = Session.GetString(PromoDiscountKey);
            decimal.TryParse(discountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var discount);

            // Voucher can never discount more than the current subtotal.
            if (discount > cart.Total)
            {
                discount = cart.Total;
            }

            cart.AppliedPromoCode = promoCode;
            cart.AppliedPromoDiscount = discount;
        }

        return cart;
    }

    /// <summary>
    /// Preferred read: falls back to the customer's DB-persisted cart (and repopulates the
    /// session from it) when the session cart is empty - e.g. after logging in from a new browser.
    /// </summary>
    public async Task<CartViewModel> GetCartAsync()
    {
        var cart = GetCart();
        if (cart.Lines.Any())
        {
            return cart;
        }

        var userId = CurrentUserId;
        if (userId is null)
        {
            return cart;
        }

        var dbLines = await LoadDbCartLinesAsync(userId);
        if (dbLines.Any())
        {
            cart.Lines = dbLines;
            SaveSessionOnly(cart);
        }

        return cart;
    }

    private void SaveSessionOnly(CartViewModel cart)
    {
        var json = JsonSerializer.Serialize(cart.Lines);
        Session.SetString(SessionKey, json);
    }

    private async Task SaveCartAsync(CartViewModel cart)
    {
        SaveSessionOnly(cart);

        var userId = CurrentUserId;
        if (userId is not null)
        {
            await PersistToDbAsync(userId, cart.Lines);
        }
    }

    private async Task<List<CartLine>> LoadDbCartLinesAsync(string userId)
    {
        var dbCart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        if (dbCart is null)
        {
            return new List<CartLine>();
        }

        return dbCart.CartItems
            .Where(ci => ci.Product is { IsActive: true })
            .Select(ci => new CartLine
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product!.Name,
                UnitPrice = ci.Product.Price,
                ImageUrl = ci.Product.ImageUrl,
                Quantity = ci.Quantity,
            })
            .ToList();
    }

    private async Task PersistToDbAsync(string userId, List<CartLine> lines)
    {
        var dbCart = await _db.Carts
            .Include(c => c.CartItems)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        if (dbCart is null)
        {
            dbCart = new Cart { UserId = userId };
            _db.Carts.Add(dbCart);
        }
        else
        {
            _db.CartItems.RemoveRange(dbCart.CartItems);
            dbCart.CartItems.Clear();
        }

        foreach (var line in lines)
        {
            dbCart.CartItems.Add(new CartItem { ProductId = line.ProductId, Quantity = line.Quantity });
        }

        await _db.SaveChangesAsync();
    }

    public void ApplyPromoCode(string promoCode, decimal discountValue)
    {
        Session.SetString(PromoCodeKey, promoCode);
        Session.SetString(PromoDiscountKey, discountValue.ToString(CultureInfo.InvariantCulture));
    }

    public void RemovePromoCode()
    {
        Session.Remove(PromoCodeKey);
        Session.Remove(PromoDiscountKey);
    }

    public async Task AddToCartAsync(int productId, int quantity = 1)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product is null || !product.IsActive)
        {
            return;
        }

        var cart = await GetCartAsync();
        var line = cart.Lines.FirstOrDefault(l => l.ProductId == productId);
        if (line is null)
        {
            cart.Lines.Add(new CartLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                ImageUrl = product.ImageUrl,
                Quantity = quantity,
            });
        }
        else
        {
            line.Quantity += quantity;
        }

        await SaveCartAsync(cart);
        await _activityTracking.TrackAsync(CurrentUserId, "AddToCart", productId);
    }

    public async Task UpdateQuantityAsync(int productId, int quantity)
    {
        var cart = await GetCartAsync();
        var line = cart.Lines.FirstOrDefault(l => l.ProductId == productId);
        if (line is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            cart.Lines.Remove(line);
        }
        else
        {
            line.Quantity = quantity;
        }

        await SaveCartAsync(cart);
        if (!cart.Lines.Any())
        {
            RemovePromoCode();
        }
    }

    public async Task RemoveFromCartAsync(int productId)
    {
        var cart = await GetCartAsync();
        cart.Lines.RemoveAll(l => l.ProductId == productId);
        await SaveCartAsync(cart);
        if (!cart.Lines.Any())
        {
            RemovePromoCode();
        }
    }

    /// <summary>Removes only the given products from the cart - used after a partial checkout
    /// (customer selected a subset of their cart to buy) so the rest of the cart survives.</summary>
    public async Task RemoveItemsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.ToHashSet();
        var cart = await GetCartAsync();
        cart.Lines.RemoveAll(l => ids.Contains(l.ProductId));
        await SaveCartAsync(cart);
        if (!cart.Lines.Any())
        {
            RemovePromoCode();
        }
    }

    // "Mua ngay" - session-only, bypasses the persisted cart entirely. Ported from ToyStore's
    // OrderController.BuyNow. Not merged/persisted to DB since it's a one-off purchase intent.
    private const string QuickBuyKey = "Cart.QuickBuy";

    public void SetQuickBuy(CartLine line)
    {
        Session.SetString(QuickBuyKey, JsonSerializer.Serialize(line));
    }

    public CartLine? GetQuickBuy()
    {
        var json = Session.GetString(QuickBuyKey);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<CartLine>(json);
    }

    public void ClearQuickBuy()
    {
        Session.Remove(QuickBuyKey);
    }

    public async Task ClearAsync()
    {
        Session.Remove(SessionKey);
        RemovePromoCode();

        var userId = CurrentUserId;
        if (userId is not null)
        {
            var dbCarts = await _db.Carts.Include(c => c.CartItems).Where(c => c.UserId == userId).ToListAsync();
            foreach (var dbCart in dbCarts)
            {
                _db.CartItems.RemoveRange(dbCart.CartItems);
                _db.Carts.Remove(dbCart);
            }
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Call right after a successful login. Merges whatever the guest had in their session
    /// cart with their previously-saved DB cart (same product -> quantities add up), then
    /// saves the merged result back to both. Ported from CartStorageService.RestoreCartAfterLoginAsync.
    /// </summary>
    public async Task MergeGuestCartIntoUserCartOnLoginAsync(string userId)
    {
        var sessionCart = GetCart();
        var dbLines = await LoadDbCartLinesAsync(userId);

        var merged = new List<CartLine>(dbLines);
        foreach (var guestLine in sessionCart.Lines)
        {
            var existing = merged.FirstOrDefault(l => l.ProductId == guestLine.ProductId);
            if (existing is not null)
            {
                existing.Quantity += guestLine.Quantity;
            }
            else
            {
                merged.Add(guestLine);
            }
        }

        var mergedCart = new CartViewModel { Lines = merged };
        SaveSessionOnly(mergedCart);
        await PersistToDbAsync(userId, merged);
    }
}
