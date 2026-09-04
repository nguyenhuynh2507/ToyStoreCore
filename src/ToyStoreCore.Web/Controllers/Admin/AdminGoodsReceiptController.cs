using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStoreCore.Web.Data;
using ToyStoreCore.Web.Models;
using ToyStoreCore.Web.Models.ViewModels;

namespace ToyStoreCore.Web.Controllers.Admin;

[Authorize(Roles = IdentitySeeder.AdminRole)]
[Route("Admin/GoodsReceipt")]
public class AdminGoodsReceiptController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminGoodsReceiptController> _logger;

    public AdminGoodsReceiptController(AppDbContext db, UserManager<ApplicationUser> userManager, ILogger<AdminGoodsReceiptController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var receipts = await _db.GoodsReceipts
            .AsNoTracking()
            .Include(r => r.CreatedBy)
            .Include(r => r.Details)
            .OrderByDescending(r => r.ImportDate)
            .ThenByDescending(r => r.Id)
            .ToListAsync();

        return View(receipts);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        await PopulateProductsAsync();
        return View(new CreateGoodsReceiptViewModel { Details = new List<GoodsReceiptLineViewModel> { new() } });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGoodsReceiptViewModel model)
    {
        var adminId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(adminId))
        {
            TempData["Error"] = "Không xác định được tài khoản Admin.";
            return RedirectToAction(nameof(Create));
        }

        model.Details = (model.Details ?? new List<GoodsReceiptLineViewModel>())
            .Where(d => d.ProductId > 0 && d.Quantity > 0)
            .ToList();

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng thêm ít nhất một dòng sản phẩm hợp lệ.");
        }

        var productIds = model.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            ModelState.AddModelError(string.Empty, "Một hoặc nhiều sản phẩm không tồn tại.");
        }

        foreach (var line in model.Details)
        {
            if (line.ImportPrice <= 0)
            {
                ModelState.AddModelError(string.Empty, "Giá nhập phải lớn hơn 0.");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateProductsAsync();
            if (model.Details.Count == 0)
            {
                model.Details.Add(new GoodsReceiptLineViewModel());
            }
            return View(model);
        }

        var totalCost = model.Details.Sum(d => d.Quantity * d.ImportPrice);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var receipt = new GoodsReceipt
            {
                CreatedByUserId = adminId,
                ImportDate = model.ImportDate.Date,
                TotalCost = totalCost,
                Note = model.Note,
            };

            foreach (var line in model.Details)
            {
                receipt.Details.Add(new GoodsReceiptDetail
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    ImportPrice = line.ImportPrice,
                });

                if (products.TryGetValue(line.ProductId, out var product))
                {
                    product.StockQuantity += line.Quantity;
                }
            }

            _db.GoodsReceipts.Add(receipt);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Message"] = $"Tạo phiếu nhập kho #{receipt.Id} thành công. Tổng chi phí: {totalCost:N0}đ";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi tạo phiếu nhập kho AdminId={AdminId}", adminId);
            ModelState.AddModelError(string.Empty, "Không thể lưu phiếu nhập kho. Vui lòng thử lại.");
            await PopulateProductsAsync();
            return View(model);
        }
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _db.GoodsReceipts
            .Include(r => r.CreatedBy)
            .Include(r => r.Details).ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receipt is null)
        {
            return NotFound();
        }

        return View(receipt);
    }

    private async Task PopulateProductsAsync()
    {
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.StockQuantity })
            .ToListAsync();

        ViewBag.Products = new SelectList(products, "Id", "Name");
        ViewBag.ProductStockMap = products.ToDictionary(p => p.Id, p => p.StockQuantity);
    }
}
