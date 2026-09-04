namespace ToyStoreCore.Web.Models;

/// <summary>
/// DB-persisted cart for logged-in customers, so the cart survives across browsers/devices.
/// Ported from ToyStore's Domain/Entities/Cart.cs + CustomerCartPersistenceService.
/// Guests still use session-only storage (see CartService) - this table is only touched
/// for authenticated users.
/// </summary>
public class Cart
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }
    public Cart? Cart { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
}
